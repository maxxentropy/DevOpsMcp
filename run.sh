#!/bin/bash

# Check if environment variables are set
if [ -z "$AZURE_DEVOPS_ORG_URL" ] || [ -z "$AZURE_DEVOPS_PAT" ]; then
    echo "Error: Please set the following environment variables:"
    echo "  export AZURE_DEVOPS_ORG_URL='https://dev.azure.com/your-org'"
    echo "  export AZURE_DEVOPS_PAT='your-personal-access-token'"
    exit 1
fi

echo "Starting DevOps MCP Server..."
echo "Organization: $AZURE_DEVOPS_ORG_URL"
echo "Mode: SSE (Server-Sent Events)"
echo ""

# Run the simple version without monitoring
docker-compose -f docker-compose.simple.yml up