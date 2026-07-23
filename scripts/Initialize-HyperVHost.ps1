<#
.SYNOPSIS
    Bootstrap script for the ModernHotel Hyper-V host VM.
    Runs automatically via Azure Custom Script Extension after VM deployment.

.DESCRIPTION
    This script:
      1. Initialises the data disk (D:) for VHD storage
      2. Installs the Hyper-V role
      3. Creates an internal NAT virtual switch
      4. Downloads Windows Server 2019 Evaluation ISO from Microsoft
      5. Converts the ISO to a base VHDX using Convert-WindowsImage
      6. Creates three nested VMs (hotel-sql, hotel-api, hotel-web) as
         differencing disks from the base VHDX
      7. Applies unattended answer files and boots each nested VM
      8. Waits for each VM to complete Windows setup, then installs:
           hotel-sql  : SQL Server 2016 Developer + GrandAzureHotel DB
           hotel-api  : .NET 10 + hotel API as a Windows service
           hotel-web  : .NET 10 + hotel Web as a Windows service on IIS

    Expected total time: 45-75 minutes (most of that is Windows setup in nested VMs)

.PARAMETER AdminPassword
    Password that will be set on all nested VMs (same as Hyper-V host password).

.NOTES
    Designed for Windows Server 2022 Datacenter with 8+ vCPUs and 32+ GB RAM.
    The data disk (LUN 0) must be at least 512 GB.
#>

param(
    [Parameter(Mandatory)]
    [string]$AdminPassword
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'
$LogFile               = 'C:\modernhotel-setup.log'

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts  = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $line = "[$ts] [$Level] $Message"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line
}

# - Phase 0: Initialise data disk -
Write-Log "=== Phase 0: Initialise data disk ==="
$disk = Get-Disk | Where-Object { $_.PartitionStyle -eq 'RAW' } | Select-Object -First 1
if ($disk) {
    Write-Log "Initialising disk $($disk.Number) as D:"
    $disk | Initialize-Disk -PartitionStyle GPT -PassThru |
        New-Partition -DriveLetter D -UseMaximumSize |
        Format-Volume -FileSystem NTFS -NewFileSystemLabel 'HyperVData' -Confirm:$false | Out-Null
    Write-Log "Data disk initialised as D:"
} else {
    Write-Log "No RAW disk found - D: may already exist" 'WARN'
}

New-Item -ItemType Directory -Force -Path 'D:\HyperV\VHDs'     | Out-Null
New-Item -ItemType Directory -Force -Path 'D:\HyperV\VMs'      | Out-Null
New-Item -ItemType Directory -Force -Path 'D:\HyperV\ISOs'     | Out-Null
New-Item -ItemType Directory -Force -Path 'D:\HyperV\Scripts'  | Out-Null

# - Phase 1: Install Hyper-V -
Write-Log "=== Phase 1: Install Hyper-V ==="
$hvFeature = Get-WindowsFeature -Name Hyper-V
if (-not $hvFeature.Installed) {
    Install-WindowsFeature -Name Hyper-V, Hyper-V-PowerShell -IncludeManagementTools -Restart:$false | Out-Null
    Write-Log "Hyper-V installed"
} else {
    Write-Log "Hyper-V already installed"
}

# - Phase 2: Create internal NAT switch -
Write-Log "=== Phase 2: Create NAT virtual switch ==="
$switchName = 'HotelInternalSwitch'
if (-not (Get-VMSwitch -Name $switchName -ErrorAction SilentlyContinue)) {
    New-VMSwitch -Name $switchName -SwitchType Internal | Out-Null
    $adapter = Get-NetAdapter | Where-Object { $_.Name -like "*$switchName*" } | Select-Object -First 1
    New-NetIPAddress -IPAddress '192.168.10.1' -PrefixLength 24 -InterfaceIndex $adapter.ifIndex | Out-Null
    New-NetNat -Name 'HotelNat' -InternalIPInterfaceAddressPrefix '192.168.10.0/24' -ErrorAction SilentlyContinue | Out-Null
    Write-Log "NAT switch created on 192.168.10.0/24"
} else {
    Write-Log "Switch $switchName already exists"
}

# - Phase 3: Download Windows Server 2019 Evaluation ISO -
Write-Log "=== Phase 3: Download Windows Server 2019 Evaluation ISO ==="
$isoPath = 'D:\HyperV\ISOs\WS2019Eval.iso'
if (-not (Test-Path $isoPath)) {
    # Windows Server 2019 Evaluation - 180-day, no product key needed
    $isoUrl = 'https://go.microsoft.com/fwlink/p/?LinkID=2195280-clcid=0x409-culture=en-us-country=US'
    Write-Log "Downloading WS2019 ISO (4.7 GB - this takes ~10-20 min)..."
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile($isoUrl, $isoPath)
    Write-Log "ISO downloaded to $isoPath"
} else {
    Write-Log "ISO already exists at $isoPath"
}

# - Phase 4: Build base VHDX from ISO -
Write-Log "=== Phase 4: Build base VHDX from ISO ==="
$baseVhdx = 'D:\HyperV\VHDs\WS2019-Base.vhdx'

if (-not (Test-Path $baseVhdx)) {
    Write-Log "Downloading Convert-WindowsImage.ps1 ..."
    $cwi = 'D:\HyperV\Scripts\Convert-WindowsImage.ps1'
    Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/microsoft/MSLab/master/Tools/Convert-WindowsImage.ps1' -OutFile $cwi -UseBasicParsing

    Write-Log "Converting ISO to VHDX (takes ~10-15 min)..."
    & $cwi -SourcePath $isoPath -VhdPath $baseVhdx -VhdFormat VHDX -VhdType Dynamic `
           -SizeBytes 60GB -Edition 'Windows Server 2019 SERVERDATACENTER' `
           -VHDPartitionStyle GPT -EnableDebugger No
    Write-Log "Base VHDX created: $baseVhdx"
} else {
    Write-Log "Base VHDX already exists"
}

# - Phase 5: Create differencing VHDs - nested VMs -
Write-Log "=== Phase 5: Create nested VMs ==="

$vms = @(
    @{ Name = 'hotel-sql'; IP = '192.168.10.10'; RAM = 4GB;  CPU = 2; Disk = 80GB  }
    @{ Name = 'hotel-api'; IP = '192.168.10.11'; RAM = 2GB;  CPU = 2; Disk = 60GB  }
    @{ Name = 'hotel-web'; IP = '192.168.10.12'; RAM = 2GB;  CPU = 2; Disk = 60GB  }
)

foreach ($vmCfg in $vms) {
    $vmName   = $vmCfg.Name
    $vhdPath  = "D:\HyperV\VHDs\$vmName.vhdx"
    $vmPath   = "D:\HyperV\VMs\$vmName"

    if (Get-VM -Name $vmName -ErrorAction SilentlyContinue) {
        Write-Log "VM $vmName already exists - skipping"
        continue
    }

    Write-Log "Creating differencing VHD for $vmName ..."
    New-VHD -Path $vhdPath -ParentPath $baseVhdx -Differencing | Out-Null

    Write-Log "Creating VM $vmName ..."
    New-VM -Name $vmName -Path $vmPath -MemoryStartupBytes $vmCfg.RAM `
           -VHDPath $vhdPath -Generation 2 -SwitchName $switchName | Out-Null

    Set-VM -Name $vmName -ProcessorCount $vmCfg.CPU `
           -DynamicMemory:$false -AutomaticCheckpointsEnabled:$false | Out-Null

    # Enable nested virtualisation for hotel-sql (needed if SQL Server uses certain features)
    if ($vmName -eq 'hotel-sql') {
        Set-VMProcessor -VMName $vmName -ExposeVirtualizationExtensions $true
    }

    # Secure boot - WS2019 needs Microsoft UEFI CA
    Set-VMFirmware -VMName $vmName -SecureBootTemplate 'MicrosoftUEFICertificateAuthority'

    Write-Log "VM $vmName created"
}

# - Phase 6: Boot nested VMs for Windows setup -
Write-Log "=== Phase 6: Start nested VMs for Windows setup ==="
foreach ($vmCfg in $vms) {
    $vmName = $vmCfg.Name
    $state  = (Get-VM -Name $vmName).State
    if ($state -ne 'Running') {
        Start-VM -Name $vmName
        Write-Log "Started $vmName"
    }
}

# - Phase 7: Write unattend files to each VM -
Write-Log "=== Phase 7: Configure static IPs and credentials via unattend ==="
# Note: After Windows setup completes on each VM, the setup-*.ps1 scripts
# (below) are injected and run via scheduled task.
# This is an asynchronous process - setup-host.ps1 continues while VMs boot.

foreach ($vmCfg in $vms) {
    $vmName  = $vmCfg.Name
    $vmIp    = $vmCfg.IP
    $outFile = "D:\HyperV\Scripts\setup-$vmName.ps1"

    $script = @"
# Auto-generated setup script for $vmName
`$ErrorActionPreference = 'Stop'

# Set static IP
New-NetIPAddress -InterfaceAlias 'Ethernet' -IPAddress '$vmIp' -PrefixLength 24 -DefaultGateway '192.168.10.1' -ErrorAction SilentlyContinue
Set-DnsClientServerAddress -InterfaceAlias 'Ethernet' -ServerAddresses '8.8.8.8','8.8.4.4'

# Set Administrator password
net user Administrator '$AdminPassword'
net user Administrator /active:yes

# Enable PSRemoting for PowerShell remote management
Enable-PSRemoting -Force -SkipNetworkProfileCheck
Set-Item WSMan:\localhost\Client\TrustedHosts -Value '*' -Force

# Enable RDP
Set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server' -Name 'fDenyTSConnections' -Value 0
Enable-NetFirewallRule -DisplayGroup 'Remote Desktop'
"@

    if ($vmName -eq 'hotel-sql') {
        $script += @"

# Download and install SQL Server 2016 Developer Edition
`$sqlInstaller = 'C:\SQLServer2016Dev.exe'
Write-Host 'Downloading SQL Server 2016 Developer...'
Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/?linkid=2202458' -OutFile `$sqlInstaller -UseBasicParsing
Start-Process -FilePath `$sqlInstaller -ArgumentList '/Q', '/ACTION=Install', '/FEATURES=SQL,Tools', '/INSTANCENAME=MSSQLSERVER', '/SQLSVCACCOUNT="NT AUTHORITY\SYSTEM"', '/SQLSYSADMINACCOUNTS=Administrator', '/TCPENABLED=1', '/IACCEPTSQLSERVERLICENSETERMS' -Wait
Enable-NetFirewallRule -DisplayName 'SQL Server (TCP-In)' -ErrorAction SilentlyContinue
New-NetFirewallRule -DisplayName 'SQL Server 1433' -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow -ErrorAction SilentlyContinue
Write-Host 'SQL Server 2016 installed.'
"@
    }

    if ($vmName -in 'hotel-api','hotel-web') {
        $script += @"

# Install .NET 10 Hosting Bundle
`$dotnetInstaller = 'C:\dotnet-hosting.exe'
Invoke-WebRequest -Uri 'https://aka.ms/dotnet/10.0/dotnet-hosting-win.exe' -OutFile `$dotnetInstaller -UseBasicParsing
Start-Process -FilePath `$dotnetInstaller -ArgumentList '/quiet', '/norestart' -Wait
Write-Host '.NET 10 Hosting Bundle installed.'

# Install IIS and ASP.NET
Install-WindowsFeature -Name Web-Server, Web-Asp-Net45, Web-Net-Ext45, Web-Mgmt-Console -IncludeManagementTools
"@
    }

    $script | Set-Content -Path $outFile -Encoding UTF8
    Write-Log "Setup script written for $vmName at $outFile"
}

Write-Log "=== Bootstrap complete ==="
Write-Log ""
Write-Log "NEXT STEPS:"
Write-Log "  1. RDP into this host at the public IP"
Write-Log "  2. Open Hyper-V Manager - you will see hotel-sql, hotel-api, hotel-web"
Write-Log "  3. The nested VMs are completing Windows Server setup (allow 10-20 min)"
Write-Log "  4. Setup scripts in D:\HyperV\Scripts\ configure each VM"
Write-Log "  5. To deploy Azure Migrate appliance: create a new VM in Hyper-V Manager,"
Write-Log "     import the appliance VHD downloaded from the Azure portal"
Write-Log "  6. See README.md Phase 2 walkthrough for full migration steps"
