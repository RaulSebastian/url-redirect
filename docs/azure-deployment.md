# Azure deployment

This repository now includes Bicep in [`infra/main.bicep`](../infra/main.bicep) to provision the Azure resources the solution expects:

- Storage account with the `redirects` Azure Table
- Azure Functions hosting plan and function app
- App Service plan and ASP.NET web app
- Log Analytics workspace and Application Insights
- Azure Front Door Standard for `/ui`, `/api/*`, and alias routing

## Prerequisites

- Azure subscription with permission to create resource groups and application resources
- Azure CLI with Bicep support
- .NET SDK that matches the project target frameworks
- `zip` available on your shell path for packaging publish output

On macOS, the shell scripts in this repository work natively:

- [`infra/validate-infra.sh`](../infra/validate-infra.sh)
- [`infra/deploy-infra.sh`](../infra/deploy-infra.sh)

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

On macOS or Linux:

```bash
./infra/validate-infra.sh
```

Notes:

- The script requires Azure CLI.
- `az deployment group validate --validation-level Template` is used so the template can be checked without provisioning resources.
- `az bicep build` only compiles the template locally. It does not create Azure resources.
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

On macOS or Linux, the equivalent commands are:

```bash
./infra/deploy-infra.sh -g rg-urlredirect-dev -e dev --what-if
./infra/deploy-infra.sh -g rg-urlredirect-dev -e dev
```

Notes:

- The deploy scripts create the target resource group if it does not already exist.
- Suggested resource group names are `rg-urlredirect-dev` and `rg-urlredirect-prod`.
- The development parameter file disables Front Door by default.

## After provisioning

This Bicep provisions infrastructure and configuration, but it does not publish the application binaries. After the resources exist, deploy:

- `src/UrlRedirect.Functions` to the Azure Functions app
- `src/UrlRedirect.Web` to the Azure Web App

Example publish commands:

```powershell
dotnet publish src/UrlRedirect.Functions/UrlRedirect.Functions.csproj -c Release
dotnet publish src/UrlRedirect.Web/UrlRedirect.Web.csproj -c Release
```

Example publish and deployment flow on macOS or Linux:

```bash
dotnet publish src/UrlRedirect.Functions/UrlRedirect.Functions.csproj -c Release -o out/functions
dotnet publish src/UrlRedirect.Web/UrlRedirect.Web.csproj -c Release -o out/web

cd out/functions && zip -r ../functions.zip .
cd ../web && zip -r ../web.zip .
cd ../..

az functionapp deployment source config-zip \
  --resource-group rg-urlredirect-dev \
  --name urlredirect-dev-func \
  --src out/functions.zip

az webapp deploy \
  --resource-group rg-urlredirect-dev \
  --name urlredirect-dev-web \
  --src-path out/web.zip \
  --type zip
```

After deployment, test:

- Web UI: `https://<web-app-name>.azurewebsites.net/ui`
- Create API: `https://<function-app-name>.azurewebsites.net/api/redirects`
- Redirect lookup: `https://<web-app-name>.azurewebsites.net/{alias}` for direct Web App access in dev, or the Front Door hostname in environments where Front Door is enabled

You can confirm infrastructure creation with:

```bash
az resource list --resource-group rg-urlredirect-dev --output table
```

If you want a custom domain on Front Door, add it after the base deployment and bind your certificate and DNS records.

## Validation status

The Bicep template and deployment scripts are version-controlled in the repository. In a later macOS deployment walkthrough, the infrastructure was deployed successfully to a development resource group, and both application packages were published and deployed. Treat this as a documented example flow rather than a guarantee for every subscription, region, or runtime combination.
