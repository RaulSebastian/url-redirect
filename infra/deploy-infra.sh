#!/usr/bin/env bash

set -euo pipefail

usage() {
  echo "Usage: $0 -g <resource-group-name> -e <dev|prod> [-l <location>] [--what-if]" >&2
}

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI ('az') is required to deploy the Bicep templates. Install Azure CLI and retry." >&2
  exit 1
fi

RESOURCE_GROUP_NAME=""
ENVIRONMENT=""
LOCATION="westeurope"
WHAT_IF="false"

while [ $# -gt 0 ]; do
  case "$1" in
    -g|--resource-group)
      RESOURCE_GROUP_NAME="${2:-}"
      shift 2
      ;;
    -e|--environment)
      ENVIRONMENT="${2:-}"
      shift 2
      ;;
    -l|--location)
      LOCATION="${2:-}"
      shift 2
      ;;
    --what-if)
      WHAT_IF="true"
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if [ -z "$RESOURCE_GROUP_NAME" ] || [ -z "$ENVIRONMENT" ]; then
  usage
  exit 1
fi

if [ "$ENVIRONMENT" != "dev" ] && [ "$ENVIRONMENT" != "prod" ]; then
  echo "Environment must be 'dev' or 'prod'." >&2
  exit 1
fi

TEMPLATE_FILE="infra/main.bicep"
PARAMETER_FILE="infra/main.${ENVIRONMENT}.parameters.json"

echo "Ensuring resource group exists: $RESOURCE_GROUP_NAME"
az group create --name "$RESOURCE_GROUP_NAME" --location "$LOCATION" >/dev/null

if [ "$WHAT_IF" = "true" ]; then
  echo "Running what-if deployment for $ENVIRONMENT"
  az deployment group what-if \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --template-file "$TEMPLATE_FILE" \
    --parameters "@${PARAMETER_FILE}"
else
  echo "Deploying infrastructure for $ENVIRONMENT"
  az deployment group create \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --template-file "$TEMPLATE_FILE" \
    --parameters "@${PARAMETER_FILE}"
fi
