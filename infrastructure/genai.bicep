// GenAI Resources: Azure OpenAI with optional AI Search
@description('Location for GenAI resources')
param location string

@description('Unique suffix for resource naming')
param uniqueSuffix string

@description('Managed Identity Principal ID for role assignments')
param managedIdentityPrincipalId string

// Use lowercase for Azure OpenAI custom subdomain name
var openAIName = toLower('oai-expensemgmt-${uniqueSuffix}')
var searchName = toLower('search-expensemgmt-${uniqueSuffix}')
var modelDeploymentName = 'gpt-4o'

// ===== Azure OpenAI =====
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIName
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIName
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// ===== GPT-4o Model Deployment =====
resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: modelDeploymentName
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    raiPolicyName: 'Microsoft.Default'
  }
}

// ===== AI Search =====
resource search 'Microsoft.Search/searchServices@2021-04-01-preview' = {
  name: searchName
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

// ===== Role Assignment: Cognitive Services OpenAI User =====
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: openAI
  name: guid(openAI.id, managedIdentityPrincipalId, 'CognitiveServicesOpenAIUser')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd') // Cognitive Services OpenAI User
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ===== Role Assignment: Search Index Data Reader =====
resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: search
  name: guid(search.id, managedIdentityPrincipalId, 'SearchIndexDataReader')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f') // Search Index Data Reader
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ===== Outputs =====
output openAIEndpoint string = openAI.properties.endpoint
output openAIName string = openAI.name
output openAIModelName string = modelDeploymentName
output searchEndpoint string = 'https://${search.name}.search.windows.net'
output searchName string = search.name
