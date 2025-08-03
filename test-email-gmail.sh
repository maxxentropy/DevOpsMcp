#!/bin/bash

echo "=== Testing Email to Gmail ===="
echo

echo "Sending test email to sean.bennington@gmail.com..."

curl -s -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "send_email",
      "arguments": {
        "to": "sean.bennington@gmail.com",
        "subject": "Test Email from DevOps MCP - Gmail",
        "templateName": "Account/Welcome",
        "templateData": {
          "Name": "Sean Bennington",
          "ActivationUrl": "https://devops-mcp.example.com/activate"
        },
        "priority": "Normal"
      }
    },
    "id": 4
  }' | jq