@description('Cosmos DB account name. Must be globally unique, 3-44 chars, lowercase + dashes.')
param accountName string

@description('Region for the account.')
param location string

@description('Resource tags.')
param tags object = {}

@description('Database name.')
param databaseName string = 'equine-booking'

var containers = [
  {
    name: 'bookings'
    partitionKey: '/spaceId'
  }
  {
    name: 'spaces'
    partitionKey: '/id'
  }
  {
    name: 'users'
    partitionKey: '/id'
  }
]

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: accountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: 'Tls12'
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: account
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource containerResources 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = [for c in containers: {
  parent: database
  name: c.name
  properties: {
    resource: {
      id: c.name
      partitionKey: {
        paths: [c.partitionKey]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
      }
      defaultTtl: -1
    }
  }
}]

@description('Cosmos account name.')
output accountName string = account.name

@description('Document endpoint for the account.')
output endpoint string = account.properties.documentEndpoint

@description('Resource ID of the account, useful for role assignments.')
output accountId string = account.id

@description('Database name.')
output databaseName string = database.name
