[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "prod")]
    [string]$Environment,

    [Parameter()]
    [string]$Location = "westeurope",

    [Parameter()]
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

function Assert-AzCli {
    $az = Get-Command az -ErrorAction SilentlyContinue

    if (-not $az) {
        throw "Azure CLI ('az') is required to deploy the Bicep templates. Install Azure CLI and retry."
    }
}

Assert-AzCli

$templateFile = ".\infra\main.bicep"
$parameterFile = ".\infra\main.$Environment.parameters.json"

Write-Host "Ensuring resource group exists: $ResourceGroupName" -ForegroundColor Cyan
az group create --name $ResourceGroupName --location $Location | Out-Null

if ($WhatIf) {
    Write-Host "Running what-if deployment for $Environment" -ForegroundColor Cyan
    az deployment group what-if `
        --resource-group $ResourceGroupName `
        --template-file $templateFile `
        --parameters "@$parameterFile"
}
else {
    Write-Host "Deploying infrastructure for $Environment" -ForegroundColor Cyan
    az deployment group create `
        --resource-group $ResourceGroupName `
        --template-file $templateFile `
        --parameters "@$parameterFile"
}
