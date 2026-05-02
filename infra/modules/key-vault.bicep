@description('Key Vault name. 3-24 chars, alphanumeric and dashes, must start with a letter.')
param vaultName string

@description('Region for the vault.')
param location string

@description('Resource tags.')
param tags object = {}

@description('Object IDs (managed identities or principals) granted Key Vault Secrets User on the vault.')
param secretsUserPrincipalIds array = []

@description('Tenant ID the vault is scoped to.')
param tenantId string = subscription().tenantId

// Built-in role: Key Vault Secrets User — read-only access to secret values.
var secretsUserRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4633458b-17de-408a-b874-0445c86b69e6'
)

resource vault 'Microsoft.KeyVault/vaults@2024-04-01-preview' = {
  name: vaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: null
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource secretsUserAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in secretsUserPrincipalIds: {
  name: guid(vault.id, principalId, secretsUserRoleId)
  scope: vault
  properties: {
    principalId: principalId
    roleDefinitionId: secretsUserRoleId
    principalType: 'ServicePrincipal'
  }
}]

@description('Key Vault name.')
output vaultName string = vault.name

@description('Vault URI, e.g. https://<name>.vault.azure.net/')
output vaultUri string = vault.properties.vaultUri

@description('Resource ID of the vault.')
output vaultId string = vault.id
