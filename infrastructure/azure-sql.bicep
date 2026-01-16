// Azure SQL Database with Entra ID-Only Authentication
@description('Location for all resources')
param location string

@description('Unique suffix for resource naming')
param uniqueSuffix string

@description('Entra ID Object ID of the administrator')
param adminObjectId string

@description('Entra ID login (UPN) of the administrator')
param adminLogin string

@description('Managed Identity Principal ID for database access')
param managedIdentityPrincipalId string

@description('Managed Identity Name for database user creation')
param managedIdentityName string

var sqlServerName = 'sql-expensemgmt-${uniqueSuffix}'
var sqlDatabaseName = 'ExpenseManagement'

// ===== SQL Server =====
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: 'sqladmin' // Required but not used with Entra ID-only
    administratorLoginPassword: guid(subscription().id, resourceGroup().id) // Required but not used
    version: '12.0'
    publicNetworkAccess: 'Enabled'
  }
}

// ===== Entra ID Administrator =====
resource sqlAdministrator 'Microsoft.Sql/servers/administrators@2021-11-01' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: adminLogin
    sid: adminObjectId
    tenantId: subscription().tenantId
    azureADOnlyAuthentication: true
  }
}

// ===== SQL Database =====
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
  }
}

// ===== Firewall Rule - Allow Azure Services =====
resource firewallRuleAzure 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ===== Outputs =====
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
