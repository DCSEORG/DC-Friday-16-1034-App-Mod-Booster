// Main orchestrator for Expense Management System
targetScope = 'resourceGroup'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Entra ID Object ID of the administrator')
param adminObjectId string

@description('Entra ID login (UPN) of the administrator')
param adminLogin string

@description('Deploy GenAI resources (Azure OpenAI)')
param deployGenAI bool = false

// Generate unique suffix for resource names
var uniqueSuffix = uniqueString(resourceGroup().id)

// ===== App Service Module =====
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
  }
}

// ===== Azure SQL Module =====
module azureSQL 'azure-sql.bicep' = {
  name: 'azureSQLDeployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
    managedIdentityName: appService.outputs.managedIdentityName
  }
}

// ===== GenAI Module (Conditional) =====
module genAI 'genai.bicep' = if (deployGenAI) {
  name: 'genAIDeployment'
  params: {
    location: 'swedencentral' // GPT-4o model location
    uniqueSuffix: uniqueSuffix
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// ===== Outputs =====
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityName string = appService.outputs.managedIdentityName
output sqlServerFqdn string = azureSQL.outputs.sqlServerFqdn
output sqlDatabaseName string = azureSQL.outputs.sqlDatabaseName
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
