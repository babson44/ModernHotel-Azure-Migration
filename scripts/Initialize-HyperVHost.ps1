<#
.SYNOPSIS
    Stage 1 bootstrap for the ModernHotel Hyper-V host VM.
    Runs via Azure Custom Script Extension (CSE) immediately after VM deployment.

.DESCRIPTION
    This script is deliberately SHORT and RELIABLE. It only does the fast,
    must-succeed host preparation, then hands the slow/fragile work off to a
    Stage 2 scheduled task that runs after a reboot:

      STAGE 1 (this script, runs in CSE, ~2-3 min):
        0. Initialise the data disk as F: (idempotent, retry-safe)
        1. Install the Hyper-V role (NOT yet functional - needs a reboot)
        2. Write the Stage 2 script to F:\HyperV\Scripts\Stage2-BuildNestedVMs.ps1
        3. Register a scheduled task that runs Stage 2 at next startup (as SYSTEM)
        4. Schedule a reboot and exit 0 so the CSE / ARM deployment reports SUCCESS

      STAGE 2 (scheduled task, runs after reboot, in the background, ~45-75 min):
        - Create the internal NAT virtual switch (Hyper-V is now live)
        - Download the Windows Server 2019 Evaluation ISO
        - Convert the ISO to a base VHDX
        - Create three nested VMs (hotel-sql, hotel-api, hotel-web)
        - Write per-VM setup scripts
        - Unregister its own scheduled task so it does not re-run

    WHY TWO STAGES:
      * Hyper-V cmdlets (New-VMSwitch/New-VM) do NOT work until the host reboots
        after the role is installed. Doing switch/VM work in the same CSE run
        always fails.
      * The ISO download + VHDX conversion + nested VM boot takes far too long to
        run synchronously inside CSE (risking a CSE timeout) and is fragile.
        Deferring it means the ARM deployment succeeds once the HOST is ready;
        the nested build then proceeds in the background and is fully logged.

.PARAMETER AdminPassword
    Password set on all nested VMs (same as the Hyper-V host password).

.NOTES
    Target: Windows Server 2022 Datacenter, 8+ vCPUs, 32+ GB RAM, D-series (nested-virt capable).
    Data disk (LUN 0) must be >= 512 GB. Azure D-series reserves D: for the temp disk,
    so the data disk is mounted as F:.
    PowerShell 5.1 compatible (Windows Server 2022 default shell). ASCII only - no
    Unicode characters (PS5.1 without a BOM misreads them and fails to parse).
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
    $ts   = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $line = "[$ts] [$Level] $Message"
    Write-Host $line
    try { Add-Content -Path $LogFile -Value $line -ErrorAction SilentlyContinue } catch { }
}

try {
    Write-Log "########## STAGE 1: Hyper-V host preparation (CSE) ##########"
    Write-Log "PowerShell version: $($PSVersionTable.PSVersion)"

    # ---------------------------------------------------------------------
    # Phase 0: Initialise data disk as F:  (idempotent + retry-safe)
    # ---------------------------------------------------------------------
    Write-Log "=== Phase 0: Initialise data disk (F:) ==="

    if (-not (Test-Path 'F:\')) {
        # Case A: a brand-new RAW disk needs initialising + formatting.
        $rawDisk = Get-Disk | Where-Object { $_.PartitionStyle -eq 'RAW' } | Select-Object -First 1
        if ($rawDisk) {
            Write-Log "Found RAW disk $($rawDisk.Number) - initialising and assigning F:"
            $rawDisk | Initialize-Disk -PartitionStyle GPT -PassThru |
                New-Partition -DriveLetter F -UseMaximumSize |
                Format-Volume -FileSystem NTFS -NewFileSystemLabel 'HyperVData' -Confirm:$false | Out-Null
        }
        else {
            # Case B: disk already GPT (e.g. a redeploy) but has no drive letter.
            Write-Log "No RAW disk - looking for an existing data partition without a drive letter" 'WARN'
            $partition = Get-Disk |
                Where-Object { $_.Number -gt 0 -and $_.PartitionStyle -eq 'GPT' } |
                Get-Partition -ErrorAction SilentlyContinue |
                Where-Object { $_.Type -eq 'Basic' -and (-not $_.DriveLetter -or $_.DriveLetter -eq [char]0) } |
                Sort-Object -Property Size -Descending |
                Select-Object -First 1
            if ($partition) {
                Write-Log "Assigning F: to existing partition on disk $($partition.DiskNumber)"
                $partition | Set-Partition -NewDriveLetter F
            }
            else {
                Write-Log "No assignable partition found; will wait to see if F: appears" 'WARN'
            }
        }

        # Wait (up to ~60s) for Windows to surface F: in the filesystem namespace.
        $tries = 0
        while (-not (Test-Path 'F:\') -and $tries -lt 12) {
            Start-Sleep -Seconds 5
            $tries++
            Write-Log "Waiting for F: to mount... ($tries/12)"
        }
    }
    else {
        Write-Log "F: already mounted - skipping disk initialisation"
    }

    if (-not (Test-Path 'F:\')) {
        throw "Data disk could not be mounted as F:. Check that a >=512 GB data disk is attached at LUN 0."
    }
    Write-Log "Data disk is available at F:"

    # Create the folder layout used by Stage 2.
    foreach ($dir in 'F:\HyperV\VHDs','F:\HyperV\VMs','F:\HyperV\ISOs','F:\HyperV\Scripts') {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
    Write-Log "Folder layout created under F:\HyperV"

    # ---------------------------------------------------------------------
    # Phase 1: Install the Hyper-V role (functional only AFTER reboot)
    # ---------------------------------------------------------------------
    Write-Log "=== Phase 1: Install Hyper-V role ==="
    $hv = Get-WindowsFeature -Name Hyper-V
    if (-not $hv.Installed) {
        Write-Log "Installing Hyper-V + management tools (no reboot yet)..."
        Install-WindowsFeature -Name Hyper-V, Hyper-V-PowerShell -IncludeManagementTools -Restart:$false | Out-Null
        Write-Log "Hyper-V role installed (reboot required to activate hypervisor)"
    }
    else {
        Write-Log "Hyper-V role already installed"
    }

    # ---------------------------------------------------------------------
    # Phase 2: Write the Stage 2 script (heavy work, runs after reboot)
    # ---------------------------------------------------------------------
    Write-Log "=== Phase 2: Write Stage 2 build script ==="
    $stage2Path = 'F:\HyperV\Scripts\Stage2-BuildNestedVMs.ps1'

    # The admin password is injected as a literal single-quoted string.
    # Escape any single quotes so the generated script stays valid.
    $pwEscaped = $AdminPassword -replace "'", "''"

    $stage2 = @'
<#
    STAGE 2 - ModernHotel nested VM builder.
    Runs as SYSTEM via the "ModernHotel-Stage2" scheduled task after the host reboots.
    Hyper-V is now live, so switch / VM creation works. Everything here is idempotent
    and each phase is wrapped so one failure does not abort the rest. Fully logged to
    F:\HyperV\stage2.log. Unregisters its own task on completion.
#>
$ErrorActionPreference = 'Continue'
$ProgressPreference    = 'SilentlyContinue'
$Stage2Log = 'F:\HyperV\stage2.log'

function S2Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts   = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $line = "[$ts] [$Level] $Message"
    Write-Host $line
    try { Add-Content -Path $Stage2Log -Value $line -ErrorAction SilentlyContinue } catch { }
}

S2Log "########## STAGE 2: Nested VM build starting ##########"

# Wait for F: to be available after boot.
$tries = 0
while (-not (Test-Path 'F:\') -and $tries -lt 24) { Start-Sleep -Seconds 5; $tries++ }
if (-not (Test-Path 'F:\')) { S2Log "FATAL: F: not available after boot" 'ERROR'; return }

# Confirm the hypervisor is actually running now.
try {
    $null = Get-VMHost -ErrorAction Stop
    S2Log "Hyper-V host service is live"
}
catch {
    S2Log "Hyper-V not responding yet - waiting 60s and retrying" 'WARN'
    Start-Sleep -Seconds 60
    try { $null = Get-VMHost -ErrorAction Stop; S2Log "Hyper-V host service is live" }
    catch { S2Log "FATAL: Hyper-V host not available: $($_.Exception.Message)" 'ERROR'; return }
}

$AdminPassword = '__ADMINPW__'
$switchName    = 'HotelInternalSwitch'
$isoPath       = 'F:\HyperV\ISOs\WS2019Eval.iso'
$baseVhdx      = 'F:\HyperV\VHDs\WS2019-Base.vhdx'

# ---- Phase 2.1: Internal NAT virtual switch ----
try {
    S2Log "=== Phase 2.1: NAT virtual switch ==="
    if (-not (Get-VMSwitch -Name $switchName -ErrorAction SilentlyContinue)) {
        New-VMSwitch -Name $switchName -SwitchType Internal | Out-Null
        Start-Sleep -Seconds 5
        $adapter = Get-NetAdapter | Where-Object { $_.Name -like "*$switchName*" } | Select-Object -First 1
        if ($adapter) {
            if (-not (Get-NetIPAddress -InterfaceIndex $adapter.ifIndex -IPAddress '192.168.10.1' -ErrorAction SilentlyContinue)) {
                New-NetIPAddress -IPAddress '192.168.10.1' -PrefixLength 24 -InterfaceIndex $adapter.ifIndex | Out-Null
            }
        }
        if (-not (Get-NetNat -Name 'HotelNat' -ErrorAction SilentlyContinue)) {
            New-NetNat -Name 'HotelNat' -InternalIPInterfaceAddressPrefix '192.168.10.0/24' | Out-Null
        }
        S2Log "NAT switch created on 192.168.10.0/24"
    }
    else { S2Log "Switch $switchName already exists" }
}
catch { S2Log "Phase 2.1 error: $($_.Exception.Message)" 'ERROR' }

# ---- Phase 2.2: Download Windows Server 2019 Evaluation ISO ----
try {
    S2Log "=== Phase 2.2: Download WS2019 Evaluation ISO ==="
    if (-not (Test-Path $isoPath) -or ((Get-Item $isoPath).Length -lt 1GB)) {
        # NOTE: this fwlink returns the Windows Server 2019 Evaluation ISO (180-day, no key).
        # The query separators MUST be '&' (a previous bug used '-' and downloaded HTML).
        $isoUrl = 'https://go.microsoft.com/fwlink/p/?LinkID=2195280&clcid=0x409&culture=en-us&country=US'
        S2Log "Downloading WS2019 ISO (~4.7 GB, 10-20 min)..."
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($isoUrl, $isoPath)
        $sizeGb = [math]::Round((Get-Item $isoPath).Length / 1GB, 2)
        if ($sizeGb -lt 1) { throw "Downloaded ISO is only $sizeGb GB - the download likely returned an error page. Aborting VHDX build." }
        S2Log "ISO downloaded ($sizeGb GB) to $isoPath"
    }
    else { S2Log "ISO already present" }
}
catch { S2Log "Phase 2.2 error: $($_.Exception.Message)" 'ERROR' }

# ---- Phase 2.3: Convert ISO to base VHDX ----
try {
    S2Log "=== Phase 2.3: Build base VHDX ==="
    if ((Test-Path $isoPath) -and ((Get-Item $isoPath).Length -ge 1GB)) {
        if (-not (Test-Path $baseVhdx)) {
            $cwi = 'F:\HyperV\Scripts\Convert-WindowsImage.ps1'
            if (-not (Test-Path $cwi)) {
                [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
                Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/microsoft/MSLab/master/Tools/Convert-WindowsImage.ps1' -OutFile $cwi -UseBasicParsing
            }
            S2Log "Converting ISO to VHDX (10-15 min)..."
            & $cwi -SourcePath $isoPath -VhdPath $baseVhdx -VhdFormat VHDX -VhdType Dynamic `
                   -SizeBytes 60GB -Edition 'Windows Server 2019 SERVERDATACENTER' `
                   -VHDPartitionStyle GPT -EnableDebugger No
            if (-not (Test-Path $baseVhdx)) { throw "Convert-WindowsImage did not produce $baseVhdx" }
            S2Log "Base VHDX created: $baseVhdx"
        }
        else { S2Log "Base VHDX already exists" }
    }
    else { S2Log "Skipping VHDX build - valid ISO not available" 'WARN' }
}
catch { S2Log "Phase 2.3 error: $($_.Exception.Message)" 'ERROR' }

# ---- Phase 2.4: Create nested VMs ----
$vms = @(
    @{ Name = 'hotel-sql'; IP = '192.168.10.10'; RAM = 4GB; CPU = 2 }
    @{ Name = 'hotel-api'; IP = '192.168.10.11'; RAM = 2GB; CPU = 2 }
    @{ Name = 'hotel-web'; IP = '192.168.10.12'; RAM = 2GB; CPU = 2 }
)

if (Test-Path $baseVhdx) {
    foreach ($vmCfg in $vms) {
        $vmName  = $vmCfg.Name
        $vhdPath = "F:\HyperV\VHDs\$vmName.vhdx"
        $vmPath  = "F:\HyperV\VMs\$vmName"
        try {
            if (Get-VM -Name $vmName -ErrorAction SilentlyContinue) { S2Log "VM $vmName already exists - skipping"; continue }
            S2Log "Creating differencing VHD + VM for $vmName ..."
            New-VHD -Path $vhdPath -ParentPath $baseVhdx -Differencing | Out-Null
            New-VM -Name $vmName -Path $vmPath -MemoryStartupBytes $vmCfg.RAM `
                   -VHDPath $vhdPath -Generation 2 -SwitchName $switchName | Out-Null
            Set-VM -Name $vmName -ProcessorCount $vmCfg.CPU `
                   -DynamicMemory:$false -AutomaticCheckpointsEnabled:$false | Out-Null
            if ($vmName -eq 'hotel-sql') {
                Set-VMProcessor -VMName $vmName -ExposeVirtualizationExtensions $true
            }
            Set-VMFirmware -VMName $vmName -SecureBootTemplate 'MicrosoftUEFICertificateAuthority'
            S2Log "VM $vmName created"
        }
        catch { S2Log "Error creating $vmName : $($_.Exception.Message)" 'ERROR' }
    }
}
else { S2Log "Skipping nested VM creation - base VHDX missing" 'WARN' }

# ---- Phase 2.5: Write per-VM setup scripts ----
try {
    S2Log "=== Phase 2.5: Write per-VM setup scripts ==="
    foreach ($vmCfg in $vms) {
        $vmName  = $vmCfg.Name
        $vmIp    = $vmCfg.IP
        $outFile = "F:\HyperV\Scripts\setup-$vmName.ps1"

        $svc = @"
# Auto-generated setup script for $vmName
`$ErrorActionPreference = 'Continue'
New-NetIPAddress -InterfaceAlias 'Ethernet' -IPAddress '$vmIp' -PrefixLength 24 -DefaultGateway '192.168.10.1' -ErrorAction SilentlyContinue
Set-DnsClientServerAddress -InterfaceAlias 'Ethernet' -ServerAddresses '8.8.8.8','8.8.4.4' -ErrorAction SilentlyContinue
net user Administrator '$AdminPassword'
net user Administrator /active:yes
Enable-PSRemoting -Force -SkipNetworkProfileCheck
Set-Item WSMan:\localhost\Client\TrustedHosts -Value '*' -Force
Set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server' -Name 'fDenyTSConnections' -Value 0
Enable-NetFirewallRule -DisplayGroup 'Remote Desktop'
"@

        if ($vmName -eq 'hotel-sql') {
            $svc += @"

# SQL Server 2016 Developer Edition
`$sqlInstaller = 'C:\SQLServer2016Dev.exe'
Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/?linkid=2202458' -OutFile `$sqlInstaller -UseBasicParsing
Start-Process -FilePath `$sqlInstaller -ArgumentList '/Q','/ACTION=Install','/FEATURES=SQL,Tools','/INSTANCENAME=MSSQLSERVER','/SQLSVCACCOUNT="NT AUTHORITY\SYSTEM"','/SQLSYSADMINACCOUNTS=Administrator','/TCPENABLED=1','/IACCEPTSQLSERVERLICENSETERMS' -Wait
New-NetFirewallRule -DisplayName 'SQL Server 1433' -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow -ErrorAction SilentlyContinue
"@
        }
        if ($vmName -in 'hotel-api','hotel-web') {
            $svc += @"

# .NET 10 Hosting Bundle + IIS
`$dotnetInstaller = 'C:\dotnet-hosting.exe'
Invoke-WebRequest -Uri 'https://aka.ms/dotnet/10.0/dotnet-hosting-win.exe' -OutFile `$dotnetInstaller -UseBasicParsing
Start-Process -FilePath `$dotnetInstaller -ArgumentList '/quiet','/norestart' -Wait
Install-WindowsFeature -Name Web-Server, Web-Asp-Net45, Web-Net-Ext45, Web-Mgmt-Console -IncludeManagementTools
"@
        }

        $svc | Set-Content -Path $outFile -Encoding UTF8
        S2Log "Setup script written for $vmName -> $outFile"
    }
}
catch { S2Log "Phase 2.5 error: $($_.Exception.Message)" 'ERROR' }

# ---- Phase 2.6: Start nested VMs ----
try {
    S2Log "=== Phase 2.6: Start nested VMs ==="
    foreach ($vmCfg in $vms) {
        $vmName = $vmCfg.Name
        $vm = Get-VM -Name $vmName -ErrorAction SilentlyContinue
        if ($vm -and $vm.State -ne 'Running') { Start-VM -Name $vmName; S2Log "Started $vmName" }
    }
}
catch { S2Log "Phase 2.6 error: $($_.Exception.Message)" 'ERROR' }

# ---- Done: remove the scheduled task so this does not run again ----
try { Unregister-ScheduledTask -TaskName 'ModernHotel-Stage2' -Confirm:$false -ErrorAction SilentlyContinue } catch { }
S2Log "########## STAGE 2 complete ##########"
'@

    # Inject the admin password into the Stage 2 script and write it out.
    $stage2 = $stage2.Replace('__ADMINPW__', $pwEscaped)
    $stage2 | Set-Content -Path $stage2Path -Encoding UTF8
    Write-Log "Stage 2 script written to $stage2Path"

    # ---------------------------------------------------------------------
    # Phase 3: Register the Stage 2 scheduled task (runs at next startup)
    # ---------------------------------------------------------------------
    Write-Log "=== Phase 3: Register Stage 2 startup task ==="
    $action    = New-ScheduledTaskAction -Execute 'powershell.exe' `
                    -Argument "-ExecutionPolicy Bypass -NonInteractive -WindowStyle Hidden -File `"$stage2Path`""
    $trigger   = New-ScheduledTaskTrigger -AtStartup
    $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
    $settings  = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
                    -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 4)
    Register-ScheduledTask -TaskName 'ModernHotel-Stage2' -Action $action -Trigger $trigger `
                    -Principal $principal -Settings $settings -Force | Out-Null
    Write-Log "Scheduled task 'ModernHotel-Stage2' registered"

    # ---------------------------------------------------------------------
    # Phase 4: Reboot to activate Hyper-V, then let Stage 2 take over
    # ---------------------------------------------------------------------
    Write-Log "=== Phase 4: Scheduling reboot in 120s ==="
    Write-Log "Stage 1 complete. Host will reboot; Stage 2 then builds the nested VMs in the background."
    Write-Log "Track progress after reboot in: F:\HyperV\stage2.log"
    shutdown /r /t 120 /c "ModernHotel: rebooting to activate Hyper-V, then building nested VMs in background"

    # Exit 0 so CSE / the ARM deployment reports SUCCESS. The reboot happens shortly after.
    exit 0
}
catch {
    Write-Log "FATAL (Stage 1): $($_.Exception.Message)" 'ERROR'
    Write-Log $_.ScriptStackTrace 'ERROR'
    exit 1
}
