// =============================================================================
// network.bicep — VNet, NSG, Azure Bastion (Basic), NIC for the Hyper-V host
// VM has no public IP — access is via Azure Bastion (browser-based RDP)
// =============================================================================
@description('Azure region')
param location string

@description('Resource name prefix')
param prefix string

var vnetName       = '${prefix}-vnet'
var subnetName     = '${prefix}-subnet'
var nsgName        = '${prefix}-nsg'
var nicName        = '${prefix}-nic'
var bastionName    = '${prefix}-bastion'
var bastionPipName = '${prefix}-bastion-pip'

// ── NSG (VM subnet — RDP handled by Bastion, no direct internet RDP) ─────────
resource nsg 'Microsoft.Network/networkSecurityGroups@2022-07-01' = {
  name: nsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'Allow-HTTP-Inbound'
        properties: {
          priority: 1010
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '80'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          description: 'HTTP access to hotel web app (nested VM NAT)'
        }
      }
      {
        name: 'Allow-HTTPS-Inbound'
        properties: {
          priority: 1020
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          description: 'HTTPS access to hotel web app (nested VM NAT)'
        }
      }
    ]
  }
}

// ── Virtual Network — two subnets: VM + AzureBastionSubnet ───────────────────
resource vnet 'Microsoft.Network/virtualNetworks@2022-07-01' = {
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
          networkSecurityGroup: { id: nsg.id }
        }
      }
      {
        // Name must be exactly 'AzureBastionSubnet' — /26 minimum
        name: 'AzureBastionSubnet'
        properties: {
          addressPrefix: '10.0.2.0/26'
        }
      }
    ]
  }
}

// ── Bastion Public IP (Standard SKU required by Azure Bastion) ────────────────
resource bastionPip 'Microsoft.Network/publicIPAddresses@2022-07-01' = {
  name: bastionPipName
  location: location
  sku: { name: 'Standard' }
  properties: {
    publicIPAllocationMethod: 'Static'
  }
}

// ── Azure Bastion — Basic SKU (lowest cost, browser-based RDP/SSH) ────────────
resource bastion 'Microsoft.Network/bastionHosts@2022-07-01' = {
  name: bastionName
  location: location
  sku: { name: 'Basic' }
  properties: {
    ipConfigurations: [
      {
        name: 'bastionIpConfig'
        properties: {
          publicIPAddress: { id: bastionPip.id }
          subnet: { id: vnet.properties.subnets[1].id }
        }
      }
    ]
  }
}

// ── NIC — private IP only; Bastion provides secure access ─────────────────────
resource nic 'Microsoft.Network/networkInterfaces@2022-07-01' = {
  name: nicName
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          privateIPAllocationMethod: 'Dynamic'
          subnet: { id: vnet.properties.subnets[0].id }
        }
      }
    ]
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output nicId       string = nic.id
output bastionName string = bastion.name
