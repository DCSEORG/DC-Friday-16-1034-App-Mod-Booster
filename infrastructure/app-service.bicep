// App Service with User-Assigned Managed Identity
@description('Location for all resources')
param location string

@description('Unique suffix for resource naming')
param uniqueSuffix string

// Get current timestamp for managed identity naming
var timestamp = utcNow('dd-HH-mm')
var managedIdentityName = 'mid-appmodassist-${timestamp}'
var appServicePlanName = 'asp-expensemgmt-${uniqueSuffix}'
var appServiceName = 'app-expensemgmt-${uniqueSuffix}'

// ===== User-Assigned Managed Identity =====
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
}

// ===== App Service Plan =====
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// ===== App Service =====
resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

// ===== Outputs =====
output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
output managedIdentityName string = managedIdentity.name
output managedIdentityId string = managedIdentity.id
