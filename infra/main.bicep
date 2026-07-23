// =============================================================================
// main.bicep — ModernHotel fake "on-premises" environment
//
// Deploys a single Windows Server 2022 VM with nested Hyper-V hosting:
//   hotel-sql  — SQL Server 2016 + GrandAzureHotel database
//   hotel-api  — ASP.NET Core Web API (.NET 10)
//   hotel-web  — ASP.NET Core MVC frontend (.NET 10)
//
// Access: Azure Bastion (Basic) — browser-based RDP, no public IP on VM
//
// After deployment (~45-60 min for nested VM setup):
//   1. Connect via Azure Bastion in the Azure portal
//   2. Deploy Azure Migrate appliance as a nested VM
//   3. Discover hotel-sql / hotel-api / hotel-web with Azure Migrate
//   4. Replicate and migrate them to Azure VMs + Azure SQL Database
// =============================================================================
targetScope = 'resourceGroup'

@description('Admin username for the Hyper-V host VM and all nested VMs')
param adminUsername string = 'hotelAdmin'

@description('Admin password (min 12 chars, must contain uppercase, lowercase, digit, special char)')
@secure()
@minLength(12)
param adminPassword string

@description('Azure region — defaults to the resource group location')
param location string = resourceGroup().location

@description('Resource name prefix')
param prefix string = 'modernhotel'

@description('VM size — must support nested virtualization (Dv3/Ev3/Dsv3 families)')
@allowed([
  'Standard_D4s_v3'
  'Standard_D8s_v3'
  'Standard_D16s_v3'
  'Standard_E4s_v3'
  'Standard_E8s_v3'
])
param vmSize string = 'Standard_D8s_v3'

@description('GitHub raw content base URL for bootstrap scripts')
param scriptsBaseUrl string = 'https://raw.githubusercontent.com/babson44/ModernHotel-Azure-Migration/main'

// ── Networking ────────────────────────────────────────────────────────────────
module network 'modules/network.bicep' = {
  name: 'network'
  params: {
    location: location
    prefix:   prefix
  }
}

// ── Hyper-V Host VM ───────────────────────────────────────────────────────────
module hypervHost 'modules/hyperv-host.bicep' = {
  name: 'hyperv-host'
  params: {
    location:       location
    prefix:         prefix
    nicId:          network.outputs.nicId
    adminUsername:  adminUsername
    adminPassword:  adminPassword
    vmSize:         vmSize
    scriptsBaseUrl: scriptsBaseUrl
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
@description('VM name — use this to connect via Azure Bastion in the portal')
output vmName string = hypervHost.outputs.vmName

@description('Azure Bastion name')
output bastionName string = network.outputs.bastionName

@description('How to connect: open Azure portal → VM → Connect → Bastion')
output connectInstructions string = 'Go to portal.azure.com → Resource Group → ${hypervHost.outputs.vmName} → Connect → Bastion'
