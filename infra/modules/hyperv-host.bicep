// =============================================================================
// hyperv-host.bicep — Windows Server 2022 VM with Hyper-V + Custom Script
// =============================================================================
@description('Azure region')
param location string

@description('Resource name prefix')
param prefix string

@description('NIC resource ID')
param nicId string

@description('Admin username')
param adminUsername string

@description('Admin password')
@secure()
param adminPassword string

@description('VM size (must support nested virtualisation)')
param vmSize string

@description('Base URL for bootstrap scripts in the GitHub repo')
param scriptsBaseUrl string

var vmName           = '${prefix}-hyperv-host'
var osDiskName       = '${vmName}-osdisk'
var scriptFileName   = 'Initialize-HyperVHost.ps1'

// ── Virtual Machine ───────────────────────────────────────────────────────────
resource vm 'Microsoft.Compute/virtualMachines@2023-09-01' = {
  name: vmName
  location: location
  properties: {
    hardwareProfile: {
      vmSize: vmSize
    }
    osProfile: {
      computerName:         vmName
      adminUsername:        adminUsername
      adminPassword:        adminPassword
      windowsConfiguration: {
        enableAutomaticUpdates: false
        provisionVMAgent:       true
        timeZone:               'UTC'
      }
    }
    storageProfile: {
      imageReference: {
        publisher: 'MicrosoftWindowsServer'
        offer:     'WindowsServer'
        sku:       '2022-datacenter-g2'
        version:   'latest'
      }
      osDisk: {
        name:         osDiskName
        createOption: 'FromImage'
        diskSizeGB:   256
        managedDisk: {
          storageAccountType: 'Premium_LRS'
        }
      }
      dataDisks: [
        {
          // Extra 512 GB disk for nested VM VHDs
          lun:          0
          name:         '${vmName}-datadisk'
          createOption: 'Empty'
          diskSizeGB:   512
          managedDisk: {
            storageAccountType: 'Premium_LRS'
          }
        }
      ]
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: nicId
        }
      ]
    }
    diagnosticsProfile: {
      bootDiagnostics: {
        enabled: true
      }
    }
  }
}

// ── Custom Script Extension — runs Initialize-HyperVHost.ps1 ─────────────────
resource cse 'Microsoft.Compute/virtualMachines/extensions@2023-09-01' = {
  parent: vm
  name:     'initialize-hyperv'
  location: location
  properties: {
    publisher:               'Microsoft.Compute'
    type:                    'CustomScriptExtension'
    typeHandlerVersion:      '1.10'
    autoUpgradeMinorVersion: true
    settings: {
      fileUris: [
        '${scriptsBaseUrl}/scripts/Initialize-HyperVHost.ps1'
      ]
    }
    protectedSettings: {
      commandToExecute: 'powershell -ExecutionPolicy Unrestricted -File ${scriptFileName} -AdminPassword "${adminPassword}"'
    }
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output vmName string = vm.name
output vmId   string = vm.id
