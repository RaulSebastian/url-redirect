# Microsoft Entra ID admin access

The web app now exposes two distinct surfaces:

- `/ui` stays public and acts as the landing page
- `/admin` is protected and requires Microsoft Entra ID sign-in

The redirect creation API at `/api/redirects` is protected by the same `AdminOnly` policy as `/admin`. That means redirect creation cannot be bypassed by calling the API directly.

## How authorization works

Authentication uses OpenID Connect against Microsoft Entra ID through `Microsoft.Identity.Web`.

Authorization uses the `AdminOnly` policy and succeeds only when:

- the user is authenticated, and
- the user has at least one configured Entra group ID in the `groups` claim, or
- the user has at least one configured app role in the `roles` claim

The policy is fail-closed. If no allowed groups or roles are configured, `/admin` and `/api/redirects` remain inaccessible.

## Recommended Entra setup

Use one of these two patterns:

1. Preferred: create an Entra security group such as `UrlRedirect.Admin` and put its object ID into `Authorization__Admin__AllowedGroupIds__0`
2. Alternative: define an app role such as `UrlRedirect.Admin` on the app registration and put that role name into `Authorization__Admin__AllowedRoles__0`

Groups are usually the simpler operational model when the same people need admin access across environments.

## App registration steps

Create or reuse an Entra app registration for the ASP.NET web app and configure:

- Platform: `Web`
- Redirect URI for local development: `https://localhost:5001/signin-oidc`
- Redirect URI for Azure App Service: `https://<your-web-app-host>/signin-oidc`
- Front-channel logout URL: `https://<your-web-app-host>/signout-callback-oidc`

Create a client secret and store it as an app setting in App Service.

## Required configuration

The web app reads these settings:

```json
{
  "Authentication": {
    "AzureAd": {
      "Instance": "https://login.microsoftonline.com/",
      "TenantId": "<tenant-guid-or-domain>",
      "ClientId": "<app-registration-client-id>",
      "ClientSecret": "<client-secret>",
      "CallbackPath": "/signin-oidc"
    }
  },
  "Authorization": {
    "Admin": {
      "AllowedGroupIds": [
        "<group-object-id>"
      ],
      "AllowedRoles": [
        "UrlRedirect.Admin"
      ]
    }
  }
}
```

In Azure App Service, use double-underscore environment variable names:

- `Authentication__AzureAd__Instance`
- `Authentication__AzureAd__TenantId`
- `Authentication__AzureAd__ClientId`
- `Authentication__AzureAd__ClientSecret`
- `Authentication__AzureAd__CallbackPath`
- `Authorization__Admin__AllowedGroupIds__0`
- `Authorization__Admin__AllowedRoles__0`

You only need to populate groups, roles, or both. At least one must be configured for admin access to work.

## Route behavior

- `/ui` is anonymous
- `/admin` challenges unauthenticated users through Entra ID
- `/api/redirects` requires the same admin authorization
- `/{alias}` remains public

## Local development

For local sign-in, either:

- set the same configuration values in local secrets or environment variables, or
- use `appsettings.Development.json` locally and keep secrets out of source control

Example user-secrets commands:

```bash
dotnet user-secrets set "Authentication:AzureAd:TenantId" "<tenant-guid>" --project src/UrlRedirect.Web/UrlRedirect.Web.csproj
dotnet user-secrets set "Authentication:AzureAd:ClientId" "<client-id>" --project src/UrlRedirect.Web/UrlRedirect.Web.csproj
dotnet user-secrets set "Authentication:AzureAd:ClientSecret" "<client-secret>" --project src/UrlRedirect.Web/UrlRedirect.Web.csproj
dotnet user-secrets set "Authorization:Admin:AllowedGroupIds:0" "<group-object-id>" --project src/UrlRedirect.Web/UrlRedirect.Web.csproj
```
