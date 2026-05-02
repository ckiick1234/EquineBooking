@description('Service Bus namespace name. Globally unique, 6-50 chars, alphanumeric + dashes.')
param namespaceName string

@description('Region for the namespace.')
param location string

@description('Resource tags.')
param tags object = {}

@description('Queue name carrying booking lifecycle events.')
param queueName string = 'booking-notifications'

resource namespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: namespace
  name: queueName
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
  }
}

@description('Service Bus namespace name.')
output namespaceName string = namespace.name

@description('Service Bus namespace resource ID, useful for role assignments.')
output namespaceId string = namespace.id

@description('Fully-qualified namespace, e.g. my-ns.servicebus.windows.net. Pass to clients using DefaultAzureCredential.')
output fullyQualifiedNamespace string = replace(replace(namespace.properties.serviceBusEndpoint, 'https://', ''), ':443/', '')

@description('Queue name.')
output queueName string = queue.name
