@description('Function app name. Globally unique, 2-60 chars, alphanumeric and dashes.')
param functionAppName string

@description('Region for all resources in this module.')
param location string

@description('Resource tags.')
param tags object = {}

@description('Storage account name for the Functions runtime. Globally unique, 3-24 lowercase alphanumeric.')
param storageAccountName string

@description('App Insights resource name.')
param appInsightsName string

@description('Log Analytics workspace name backing App Insights.')
param logAnalyticsName string

@description('Cosmos DB document endpoint. Used by the API to connect via managed identity.')
param cosmosEndpoint string

@description('Cosmos DB database name.')
param cosmosDatabaseName string

@description('Key Vault URI. Used to wire up Key Vault references in app settings.')
param keyVaultUri string

@description('Service Bus fully-qualified namespace, e.g. my-ns.servicebus.windows.net. Used by the API for keyless access via managed identity.')
param serviceBusFullyQualifiedNamespace string

@description('Value for ASPNETCORE_ENVIRONMENT. Use "Development" in dev to relax the seed-endpoint auth check; "Production" otherwise.')
@allowed([
  'Development'
  'Staging'
  'Production'
])
param aspNetCoreEnvironment string = 'Production'

@description('Allowed CORS origin (the Static Web App URL).')
param allowedOrigin string

@description('Microsoft Entra External ID tenant subdomain, e.g. "wardranch" for wardranch.ciamlogin.com.')
param entraTenantSubdomain string

@description('Microsoft Entra External ID tenant ID (GUID).')
param entraTenantId string

@description('Application (client) ID of the API app registered in the Entra External ID tenant.')
param entraApiClientId string

@description('Admin email surfaced to the API for notification routing.')
param adminEmail string

@description('Venmo link surfaced to the API for booking-confirmation emails.')
param venmoLink string

@description('Additional app settings to merge in. Use this for Key Vault references, e.g. { SendGridKey: "@Microsoft.KeyVault(VaultName=...;SecretName=...)" }.')
param additionalAppSettings object = {}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${functionAppName}-plan'
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true
  }
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

var coreAppSettings = [
  { name: 'AzureWebJobsStorage', value: storageConnectionString }
  { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
  { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
  { name: 'ASPNETCORE_ENVIRONMENT', value: aspNetCoreEnvironment }
  // Azure Files-related settings (WEBSITE_MOUNT_ENABLED, WEBSITE_CONTENTAZUREFILECONNECTIONSTRING,
  // WEBSITE_CONTENTSHARE) intentionally omitted: on Linux Consumption (Y1) with
  // WEBSITE_RUN_FROM_PACKAGE pointing at a SAS URL, the Azure Files mount is unused. Setting any
  // subset of them triggers "Invalid values supplied for Azure Files related app settings" on
  // re-deploy. WEBSITE_RUN_FROM_PACKAGE is also omitted — the func CLI sets it to a SAS URL.
  { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
  { name: 'ApplicationInsightsAgent_EXTENSION_VERSION', value: '~3' }
  { name: 'CosmosDb__Endpoint', value: cosmosEndpoint }
  { name: 'CosmosDb__DatabaseName', value: cosmosDatabaseName }
  { name: 'ServiceBus__FullyQualifiedNamespace', value: serviceBusFullyQualifiedNamespace }
  { name: 'ServiceBus__fullyQualifiedNamespace', value: serviceBusFullyQualifiedNamespace }
  { name: 'KeyVaultUri', value: keyVaultUri }
  { name: 'AzureAd__Instance', value: 'https://${entraTenantSubdomain}.ciamlogin.com/' }
  { name: 'AzureAd__TenantId', value: entraTenantId }
  { name: 'AzureAd__ClientId', value: entraApiClientId }
  { name: 'AzureAd__Audience', value: 'api://${entraApiClientId}' }
  { name: 'AdminEmail', value: adminEmail }
  { name: 'VenmoLink', value: venmoLink }
]

var extraAppSettings = [for setting in items(additionalAppSettings): {
  name: setting.key
  value: string(setting.value)
}]

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    keyVaultReferenceIdentity: 'SystemAssigned'
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true
      use32BitWorkerProcess: false
      cors: {
        allowedOrigins: [
          allowedOrigin
        ]
        supportCredentials: false
      }
      appSettings: concat(coreAppSettings, extraAppSettings)
    }
  }
}

@description('Function app name.')
output functionAppName string = functionApp.name

@description('Function app default hostname (https://...).')
output functionAppUrl string = 'https://${functionApp.properties.defaultHostName}'

@description('Function app system-assigned identity object ID. Use for role assignments.')
output principalId string = functionApp.identity.principalId

@description('App Insights resource name.')
output appInsightsName string = appInsights.name

@description('Storage account name backing the function app.')
output storageAccountName string = storage.name
