@description('Static Web App name.')
param staticWebAppName string

@description('Region for the SWA. Note SWA Free is only available in a subset of regions; common picks: eastus2, centralus, westus2, westeurope, eastasia.')
param location string

@description('Resource tags.')
param tags object = {}

@description('GitHub repository URL, e.g. https://github.com/org/repo. Leave empty to deploy without source control link.')
param repositoryUrl string = ''

@description('Branch the SWA deploys from, e.g. main.')
param branch string = 'main'

@description('GitHub personal access token with repo + workflow scopes. Required when repositoryUrl is set. Pass at deploy time, do not commit.')
@secure()
param repositoryToken string = ''

@description('Path within the repo where the Angular app lives.')
param appLocation string = 'src/frontend'

@description('Output path produced by the Angular build, relative to appLocation.')
param outputLocation string = 'dist/equine-booking-frontend'

@description('Path within the repo where the API lives. Leave empty when deploying the API separately to Azure Functions.')
param apiLocation string = ''

var hasRepo = !empty(repositoryUrl)

resource swa 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    repositoryUrl: hasRepo ? repositoryUrl : null
    branch: hasRepo ? branch : null
    repositoryToken: hasRepo ? repositoryToken : null
    buildProperties: {
      appLocation: appLocation
      apiLocation: apiLocation
      outputLocation: outputLocation
    }
    provider: hasRepo ? 'GitHub' : 'None'
  }
}

@description('Static Web App name.')
output staticWebAppName string = swa.name

@description('Default hostname (without scheme), e.g. nice-pebble-1234.azurestaticapps.net.')
output defaultHostname string = swa.properties.defaultHostname

@description('Default URL with https scheme.')
output defaultUrl string = 'https://${swa.properties.defaultHostname}'
