#!/bin/bash
set -o allexport
source ${BASH_SOURCE%/*}/../.env set
set -e

dotnet user-secrets clear --project=${BASH_SOURCE%/*}/../src/Api

dotnet user-secrets set "OpenAIServiceOptions:ApiKey" "${OPEN_AI_API_KEY}" --project=${BASH_SOURCE%/*}/../src/Api
dotnet user-secrets set "OpenAIServiceOptions:Organization" "${OPEN_AI_ORG_ID}" --project=${BASH_SOURCE%/*}/../src/Api
