# Azure deployment

This repository now includes Bicep in [`infra/main.bicep`](../infra/main.bicep) to provision the Azure resources the solution expects:

- Storage account with the `redirects` Azure Table
- Azure Functions hosting plan and function app
- App Service plan and ASP.NET web app
- Log Analytics workspace and Application Insights
- Azure Front Door Standard for `/ui`, `/api/*`, and alias routing

## What the Bicep does

The deployment wires the same storage account into both application hosts:

- `AzureWebJobsStorage` is set on the function app for host storage
- `ConnectionStrings__RedirectStorage` is set on both the function app and web app for redirect persistence
- `RedirectStorage__TableName` defaults to `redirects`

Azure Front Door is optional and controlled by the `deployFrontDoor` parameter. When enabled, the routes are:

- `/`, `/ui`, `/ui/*` -> ASP.NET web app
- `/api/*` -> Azure Functions
- `/*` -> Azure Functions for redirect alias lookups

## Deploy

Create a resource group if needed:

```powershell
az group create --name rg-urlredirect-dev --location westeurope
```

Deploy the infrastructure:

```powershell
az deployment group create `
  --resource-group rg-urlredirect-dev `
  --template-file infra/main.bicep `
  --parameters @infra/main.parameters.json
```

## After provisioning

This Bicep provisions infrastructure and configuration, but it does not publish the application binaries. After the resources exist, deploy:

- `src/UrlRedirect.Functions` to the Azure Functions app
- `src/UrlRedirect.Web` to the Azure Web App

If you want a custom domain on Front Door, add it after the base deployment and bind your certificate and DNS records.
