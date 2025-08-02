#!/bin/bash

# Test authentication diagnostics endpoint

echo "Testing authentication diagnostics endpoint..."
echo "============================================"

# Test locally (if running)
if curl -s http://localhost:8080/health > /dev/null 2>&1; then
    echo "Local server is running. Testing local endpoint:"
    curl -s http://localhost:8080/debug/auth | jq '.'
else
    echo "Local server not running. Testing Docker container..."
    
    # Check if container is running
    if docker ps | grep -q devops-mcp-server; then
        echo "Container is running. Testing container endpoint:"
        docker exec devops-mcp-server curl -s http://localhost:8080/debug/auth | jq '.'
    else
        echo "Container not running. Please start the container first."
        echo "Run: docker-compose up -d"
    fi
fi

echo ""
echo "============================================"
echo "To test authentication, ensure you have set:"
echo "  - AZURE_DEVOPS_ORG_URL environment variable"
echo "  - AZURE_DEVOPS_PAT environment variable"
echo ""
echo "Or create a .env file from .env.example"