[CmdletBinding()]
param(
    [Parameter()]
    [string]$TemplateFile = ".\infra\main.bicep",

    [Parameter()]
    [string[]]$ParameterFiles = @(
        ".\infra\main.dev.parameters.json",
        ".\infra\main.prod.parameters.json"
    )
)

$ErrorActionPreference = "Stop"

function Assert-AzCli {
    $az = Get-Command az -ErrorAction SilentlyContinue

    if (-not $az) {
        throw "Azure CLI ('az') is required to validate the Bicep templates. Install Azure CLI and retry."
    }
}

Assert-AzCli

Write-Host "Building Bicep template: $TemplateFile" -ForegroundColor Cyan
az bicep build --file $TemplateFile | Out-Null

foreach ($parameterFile in $ParameterFiles) {
    Write-Host "Validating parameter file: $parameterFile" -ForegroundColor Cyan
    az deployment group validate `
        --resource-group "validation-placeholder-rg" `
        --template-file $TemplateFile `
        --parameters "@$parameterFile" `
        --validation-level Template | Out-Null
}

Write-Host "Infrastructure validation completed successfully." -ForegroundColor Green
