#!/bin/bash

echo "=== Testing Email Tools ==="
echo

# Test 1: List all tools to see email tools
echo "1. Listing email tools..."
curl -s -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "params": {},
    "id": 1
  }' | jq '.result.tools[] | select(.name | contains("email")) | {name, description}'

echo
echo "2. Testing email preview with Welcome template..."

# Try with object notation
curl -s -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "preview_email",
      "arguments": {
        "templateName": "Account/Welcome",
        "templateData": {
          "Name": "Test User",
          "ActivationUrl": "https://example.com/activate"
        },
        "format": "both"
      }
    },
    "id": 2
  }' | jq

echo
echo "3. Testing send email (will fail if email not verified in SES)..."

curl -s -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "send_email",
      "arguments": {
        "to": "sbennington@val-co.com",
        "subject": "Test Email from DevOps MCP",
        "templateName": "Account/Welcome",
        "templateData": {
          "Name": "Sean Bennington",
          "ActivationUrl": "https://devops-mcp.example.com/activate"
        },
        "priority": "Normal"
      }
    },
    "id": 3
  }' | jq