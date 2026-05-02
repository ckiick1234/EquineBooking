using './main.bicep'

param environmentName = 'prod'
param location = 'westus2'
param staticWebAppLocation = 'westus2'

param b2cTenantName = 'your-b2c-tenant'
param adminEmail = 'admin@example.com'
param venmoLink = 'https://venmo.com/u/your-handle'

// Pass repositoryToken at deploy time: --parameters repositoryToken=$GH_PAT
param repositoryUrl = ''
param branch = 'main'
