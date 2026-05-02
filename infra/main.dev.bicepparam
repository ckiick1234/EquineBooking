using './main.bicep'

param environmentName = 'dev'
param location = 'westus2'
param staticWebAppLocation = 'westus2'

param entraTenantSubdomain = 'WardRanch'
param entraTenantId = '6476505d-d82a-41d7-bc66-a8b7cea00459'
param entraApiClientId = 'e33b40d1-72b6-4616-9294-e7ee7b4fd053'

param adminEmail = 'chris.kiick.nw@gmail.com'

// TODO: replace placeholder with the real Venmo profile link before going live.
param venmoLink = 'https://venmo.com/u/your-handle'

// Leave empty for initial bring-up; set to https://github.com/ckiick1234/WardRanch
// once a GitHub PAT (repo + workflow scopes) is minted, then pass it at deploy
// time: --parameters repositoryToken=$GH_PAT
param repositoryUrl = ''
param branch = 'main'
