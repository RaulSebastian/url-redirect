#!/usr/bin/env bash

set -euo pipefail

TEMPLATE_FILE="${1:-infra/main.bicep}"
shift $(( $# > 0 ? 1 : 0 )) || true

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI ('az') is required to validate the Bicep templates. Install Azure CLI and retry." >&2
  exit 1
fi

PARAMETER_FILES=("$@")

if [ ${#PARAMETER_FILES[@]} -eq 0 ]; then
  PARAMETER_FILES=(
    "infra/main.dev.parameters.json"
    "infra/main.prod.parameters.json"
  )
fi

echo "Building Bicep template: $TEMPLATE_FILE"
az bicep build --file "$TEMPLATE_FILE" >/dev/null

for parameter_file in "${PARAMETER_FILES[@]}"; do
  echo "Validating parameter file: $parameter_file"
  az deployment group validate \
    --resource-group "validation-placeholder-rg" \
    --template-file "$TEMPLATE_FILE" \
    --parameters "@${parameter_file}" \
    --validation-level Template \
    >/dev/null
done

echo "Infrastructure validation completed successfully."
