// =============================================================================
// network.bicep — VNet, Subnet, NSG, Public IP, NIC for the Hyper-V host
// =============================================================================
@description('Azure region')
param location string

@description('Resource name prefix')
param prefix string

var vnetName       = '${prefix}-vnet'
var subnetName     = '${prefix}-subnet'
var nsgName        = '${prefix}-nsg'
var publicIpName   = '${prefix}-pip'
var nicName        = '${prefix}-nic'

// ── NSG ──────────────────────────────────────────────────────────────────────
resource nsg 'Microsoft.Network/networkSecurityGroups@2023-09-01' = {
  name: nsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'Allow-RDP'
        properties: {
          priority: 1000
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '3389'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          description: 'Allow RDP access to Hyper-V host'
        }
      }
      {
        name: 'Allow-HTTP'
        properties: {
          priority: 1010
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '80'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          description: 'Allow HTTP to hotel web app'
        }
      }
      {
        name: 'Allow-HTTPS'
        properties: {
          priority: 1020
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          description: 'Allow HTTPS to hotel web app'
        }
      }
    ]
  }
}

// ── Virtual Network ───────────────────────────────────────────────────────────
resource vnet 'Microsoft.Network/virtualNetworks@2023-09-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: ['10.0.0.0/16']
    }
    subnets: [
      {
        name: subnetName
        properties: {
          addressPrefix: '10.0.1.0/24'
          networkSecurityGroup: {
            id: nsg.id
          }
        }
      }
    ]
  }
}

// ── Public IP ─────────────────────────────────────────────────────────────────
resource publicIp 'Microsoft.Network/publicIPAddresses@2023-09-01' = {
  name: publicIpName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    dnsSettings: {
      domainNameLabel: toLower('${prefix}-hotel')
    }
  }
}

// ── NIC ───────────────────────────────────────────────────────────────────────
resource nic 'Microsoft.Network/networkInterfaces@2023-09-01' = {
  name: nicName
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          privateIPAllocationMethod: 'Dynamic'
          publicIPAddress: {
            id: publicIp.id
          }
          subnet: {
            id: vnet.properties.subnets[0].id
          }
        }
      }
    ]
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output nicId         string = nic.id
output publicIpFqdn  string = publicIp.properties.dnsSettings.fqdn
output publicIpAddr  string = publicIp.properties.ipAddress
