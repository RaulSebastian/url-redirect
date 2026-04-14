targetScope = 'resourceGroup'

@description('Short environment suffix used in resource names, for example dev, test, or prod.')
param environmentName string = 'dev'

@description('Primary Azure region for the deployment.')
param location string = resourceGroup().location

@description('Base application name used to derive resource names.')
param appName string = 'urlredirect'

@description('Name of the redirects table.')
param redirectsTableName string = 'redirects'

@description('App Service SKU for the web frontend plan.')
param webPlanSkuName string = 'B1'

@description('App Service SKU tier for the web frontend plan.')
param webPlanSkuTier string = 'Basic'

@description('Whether to create Azure Front Door for unified routing across the web app and functions app.')
param deployFrontDoor bool = true

@description('Whether to enable Azure Front Door session affinity on origin groups.')
param frontDoorSessionAffinity bool = false

var resourceToken = toLower(replace('${appName}${environmentName}${uniqueString(resourceGroup().id)}', '-', ''))
var storageAccountName = take('st${resourceToken}', 24)
var functionAppName = take('${appName}-${environmentName}-func', 60)
var webPlanName = take('${appName}-${environmentName}-web-plan', 40)
var webAppName = take('${appName}-${environmentName}-web', 60)
var functionPlanName = take('${appName}-${environmentName}-func-plan', 40)
var appInsightsName = take('${appName}-${environmentName}-appi', 260)
var logAnalyticsName = take('${appName}-${environmentName}-logs', 63)
var frontDoorProfileName = take('${appName}-${environmentName}-afd', 260)
var frontDoorEndpointName = take(replace('${appName}-${environmentName}-${uniqueString(resourceGroup().id)}', '-', ''), 46)

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  name: 'default'
  parent: storageAccount
}

resource redirectsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  name: redirectsTableName
  parent: tableService
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource functionPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: functionPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: 'functionapp'
  properties: {
    reserved: false
  }
}

resource webPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: webPlanName
  location: location
  sku: {
    name: webPlanSkuName
    tier: webPlanSkuTier
  }
  kind: 'app'
  properties: {
    reserved: false
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionPlan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'ConnectionStrings__RedirectStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'RedirectStorage__TableName'
          value: redirectsTableName
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
      ]
    }
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: webPlan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      alwaysOn: true
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ConnectionStrings__RedirectStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'RedirectStorage__TableName'
          value: redirectsTableName
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
      ]
    }
  }
}

resource frontDoorProfile 'Microsoft.Cdn/profiles@2024-02-01' = if (deployFrontDoor) {
  name: frontDoorProfileName
  location: 'global'
  sku: {
    name: 'Standard_AzureFrontDoor'
  }
}

resource frontDoorEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2024-02-01' = if (deployFrontDoor) {
  name: frontDoorEndpointName
  parent: frontDoorProfile
  location: 'global'
  properties: {
    enabledState: 'Enabled'
  }
}

resource webOriginGroup 'Microsoft.Cdn/profiles/originGroups@2024-02-01' = if (deployFrontDoor) {
  name: 'web-origin-group'
  parent: frontDoorProfile
  properties: {
    sessionAffinityState: frontDoorSessionAffinity ? 'Enabled' : 'Disabled'
    healthProbeSettings: {
      probePath: '/health'
      probeRequestType: 'GET'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 120
    }
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
      additionalLatencyInMilliseconds: 50
    }
  }
}

resource functionsOriginGroup 'Microsoft.Cdn/profiles/originGroups@2024-02-01' = if (deployFrontDoor) {
  name: 'functions-origin-group'
  parent: frontDoorProfile
  properties: {
    sessionAffinityState: 'Disabled'
    healthProbeSettings: {
      probePath: '/health'
      probeRequestType: 'GET'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 120
    }
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
      additionalLatencyInMilliseconds: 50
    }
  }
}

resource webOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2024-02-01' = if (deployFrontDoor) {
  name: 'web-app-origin'
  parent: webOriginGroup
  properties: {
    hostName: webApp.properties.defaultHostName
    originHostHeader: webApp.properties.defaultHostName
    httpPort: 80
    httpsPort: 443
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
  }
}

resource functionsOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2024-02-01' = if (deployFrontDoor) {
  name: 'functions-app-origin'
  parent: functionsOriginGroup
  properties: {
    hostName: functionApp.properties.defaultHostName
    originHostHeader: functionApp.properties.defaultHostName
    httpPort: 80
    httpsPort: 443
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
  }
}

resource webRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2024-02-01' = if (deployFrontDoor) {
  name: 'web-route'
  parent: frontDoorEndpoint
  properties: {
    originGroup: {
      id: webOriginGroup.id
    }
    supportedProtocols: [
      'Http'
      'Https'
    ]
    patternsToMatch: [
      '/'
      '/ui'
      '/ui/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
  }
}

resource apiRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2024-02-01' = if (deployFrontDoor) {
  name: 'api-route'
  parent: frontDoorEndpoint
  properties: {
    originGroup: {
      id: functionsOriginGroup.id
    }
    supportedProtocols: [
      'Http'
      'Https'
    ]
    patternsToMatch: [
      '/api/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
  }
}

resource aliasRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2024-02-01' = if (deployFrontDoor) {
  name: 'alias-route'
  parent: frontDoorEndpoint
  properties: {
    originGroup: {
      id: functionsOriginGroup.id
    }
    supportedProtocols: [
      'Http'
      'Https'
    ]
    patternsToMatch: [
      '/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
  }
}

output storageAccountName string = storageAccount.name
output redirectsTableResourceId string = redirectsTable.id
output webAppName string = webApp.name
output webAppDefaultHostName string = webApp.properties.defaultHostName
output functionAppName string = functionApp.name
output functionAppDefaultHostName string = functionApp.properties.defaultHostName
output applicationInsightsName string = applicationInsights.name
output frontDoorEndpointHostName string = deployFrontDoor ? frontDoorEndpoint!.properties.hostName : ''
