targetScope = 'resourceGroup'

@description('Environment short name, used in every resource name.')
@allowed([
  'dev'
  'prod'
])
param environmentName string

@description('Region for all resources.')
param location string = resourceGroup().location

@description('Static Web App region. Free tier is only available in a subset of regions; defaults to westus2.')
param staticWebAppLocation string = 'westus2'

@description('Microsoft Entra External ID tenant subdomain, e.g. "wardranch" for wardranch.ciamlogin.com / wardranch.onmicrosoft.com.')
param entraTenantSubdomain string = 'WardRanch'

@description('Microsoft Entra External ID tenant ID (GUID).')
param entraTenantId string

@description('Application (client) ID of the API app registered in the Entra External ID tenant.')
param entraApiClientId string

@description('Admin email address surfaced to the API for booking notifications.')
param adminEmail string = 'chris.kiick.nw@gmail.com'

@description('Venmo profile link surfaced to the API for booking-confirmation emails.')
param venmoLink string

@description('GitHub repository URL the Static Web App pulls from. Leave empty to skip source-control wiring.')
param repositoryUrl string = ''

@description('GitHub branch the Static Web App deploys from.')
param branch string = 'main'

@description('GitHub personal access token (repo + workflow scopes). Required when repositoryUrl is set. Pass via --parameters at deploy time, do not commit.')
@secure()
param repositoryToken string = ''

@description('Tags applied to every resource.')
param tags object = {
  workload: 'equine-booking'
  environment: environmentName
}

// Short hash to keep globally-unique names predictable per (subscription, RG, env).
var nameSuffix = take(uniqueString(subscription().id, resourceGroup().id, environmentName), 6)

var cosmosName = 'cosmos-equine-${environmentName}-${nameSuffix}'
var keyVaultName = 'kv-equine-${environmentName}-${nameSuffix}'
var functionAppName = 'func-equine-${environmentName}-${nameSuffix}'
var storageName = 'stequine${environmentName}${nameSuffix}'
var appInsightsName = 'ai-equine-${environmentName}'
var logAnalyticsName = 'log-equine-${environmentName}'
var staticWebAppName = 'swa-equine-${environmentName}-${nameSuffix}'
var serviceBusName = 'sb-equine-${environmentName}-${nameSuffix}'

// Built-in role IDs.
var keyVaultSecretsUserRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4633458b-17de-408a-b874-0445c86b69e6'
)
var cosmosDataContributorRoleId = '00000000-0000-0000-0000-000000000002'
var serviceBusDataOwnerRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '090c5cfd-751d-490a-894a-3ce6f1109419'
)

module cosmos 'modules/cosmos.bicep' = {
  name: 'cosmos'
  params: {
    accountName: cosmosName
    location: location
    tags: tags
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault'
  params: {
    vaultName: keyVaultName
    location: location
    tags: tags
  }
}

module serviceBus 'modules/service-bus.bicep' = {
  name: 'serviceBus'
  params: {
    namespaceName: serviceBusName
    location: location
    tags: tags
  }
}

module functions 'modules/functions.bicep' = {
  name: 'functions'
  params: {
    functionAppName: functionAppName
    location: location
    tags: tags
    storageAccountName: storageName
    appInsightsName: appInsightsName
    logAnalyticsName: logAnalyticsName
    cosmosEndpoint: cosmos.outputs.endpoint
    cosmosDatabaseName: cosmos.outputs.databaseName
    keyVaultUri: keyVault.outputs.vaultUri
    serviceBusFullyQualifiedNamespace: serviceBus.outputs.fullyQualifiedNamespace
    aspNetCoreEnvironment: environmentName == 'dev' ? 'Development' : 'Production'
    allowedOrigin: staticWebApp.outputs.defaultUrl
    entraTenantSubdomain: entraTenantSubdomain
    entraTenantId: entraTenantId
    entraApiClientId: entraApiClientId
    adminEmail: adminEmail
    venmoLink: venmoLink
  }
}

module staticWebApp 'modules/static-web-app.bicep' = {
  name: 'staticWebApp'
  params: {
    staticWebAppName: staticWebAppName
    location: staticWebAppLocation
    tags: tags
    repositoryUrl: repositoryUrl
    branch: branch
    repositoryToken: repositoryToken
  }
}

// Grant the function app's managed identity access to read Key Vault secrets.
// existing references use the literal var names so the role-assignment scope/name
// can be resolved at the start of deployment.
resource kv 'Microsoft.KeyVault/vaults@2024-04-01-preview' existing = {
  name: keyVaultName
}

resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(kv.id, functionAppName, keyVaultSecretsUserRoleId)
  scope: kv
  properties: {
    principalId: functions.outputs.principalId
    roleDefinitionId: keyVaultSecretsUserRoleId
    principalType: 'ServicePrincipal'
  }
}

// Grant the function app's managed identity Cosmos data-plane access.
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosName
}

resource cosmosRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: cosmosAccount
  name: guid(cosmosAccount.id, functionAppName, cosmosDataContributorRoleId)
  properties: {
    principalId: functions.outputs.principalId
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/${cosmosDataContributorRoleId}'
    scope: cosmosAccount.id
  }
}

// Grant the function app's managed identity send + receive on the Service Bus namespace.
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: serviceBusName
}

resource serviceBusRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, functionAppName, serviceBusDataOwnerRoleId)
  scope: serviceBusNamespace
  properties: {
    principalId: functions.outputs.principalId
    roleDefinitionId: serviceBusDataOwnerRoleId
    principalType: 'ServicePrincipal'
  }
}

@description('Function app HTTPS URL.')
output functionAppUrl string = functions.outputs.functionAppUrl

@description('Static Web App HTTPS URL.')
output staticWebAppUrl string = staticWebApp.outputs.defaultUrl

@description('Key Vault name. Use this when adding secrets that the function app should read via Key Vault references.')
output keyVaultName string = keyVault.outputs.vaultName

@description('Key Vault URI.')
output keyVaultUri string = keyVault.outputs.vaultUri

@description('Cosmos DB account name.')
output cosmosAccountName string = cosmos.outputs.accountName

@description('Cosmos DB endpoint.')
output cosmosEndpoint string = cosmos.outputs.endpoint
