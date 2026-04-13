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

## Environment configuration

The repository includes two checked-in parameter files:

- Development: [`infra/main.dev.parameters.json`](../infra/main.dev.parameters.json)
- Production: [`infra/main.prod.parameters.json`](../infra/main.prod.parameters.json)

The defaults are intentionally different:

- Development uses a `Basic` web plan and disables Front Door to keep costs and moving parts lower.
- Production uses a `PremiumV3` web plan and enables Front Door for the single-host routing model described in the README.

Adjust the values to match your subscription, region, and cost targets before deployment.

## Validate

Before deploying, validate the template and parameter files:

```powershell
./infra/validate-infra.ps1
```

Notes:

- The script requires Azure CLI.
- `az deployment group validate --validation-level Template` is used so the template can be checked without provisioning resources.
- You may still want to run `what-if` against a real resource group before production changes.

## Deploy

Use the deployment script for a repeatable flow:

```powershell
./infra/deploy-infra.ps1 -ResourceGroupName rg-urlredirect-dev -Environment dev -WhatIf
```

Apply the development deployment:

```powershell
./infra/deploy-infra.ps1 -ResourceGroupName rg-urlredirect-dev -Environment dev
```

Apply the production deployment:

```powershell
./infra/deploy-infra.ps1 -ResourceGroupName rg-urlredirect-prod -Environment prod
```

## After provisioning

This Bicep provisions infrastructure and configuration, but it does not publish the application binaries. After the resources exist, deploy:

- `src/UrlRedirect.Functions` to the Azure Functions app
- `src/UrlRedirect.Web` to the Azure Web App

Example publish commands:

```powershell
dotnet publish src/UrlRedirect.Functions/UrlRedirect.Functions.csproj -c Release
dotnet publish src/UrlRedirect.Web/UrlRedirect.Web.csproj -c Release
```

If you want a custom domain on Front Door, add it after the base deployment and bind your certificate and DNS records.

## Validation status

The scripts and templates are version-controlled in the repository. In this coding session they were updated and reviewed, but they were not executed end-to-end here because Azure CLI is not installed in the current environment. Run `./infra/validate-infra.ps1` locally or in CI to complete the verification step.
