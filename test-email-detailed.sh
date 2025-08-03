#!/bin/bash

echo "=== Detailed Email Test with Headers ==="
echo

# First, let's send an email with more headers that might help with Office 365
echo "Sending email with additional headers for Office 365..."

curl -s -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "send_email",
      "arguments": {
        "to": "sbennington@val-co.com",
        "subject": "DevOps MCP Test - Check All Folders",
        "templateName": "Account/Welcome",
        "templateData": {
          "Name": "Sean Bennington",
          "ActivationUrl": "https://devops-mcp.example.com/activate"
        },
        "priority": "Normal",
        "tags": {
          "purpose": "testing",
          "sender": "devops-mcp"
        }
      }
    },
    "id": 5
  }' | jq

echo
echo "=== To check in Office 365: ==="
echo "1. Check Junk Email folder"
echo "2. Check Quarantine (https://security.microsoft.com/quarantine)"
echo "3. Check 'Other' or 'Clutter' folders"
echo "4. Search all folders for: from:sbennington@val-co.com"
echo
echo "=== To improve delivery: ==="
echo "1. Add sbennington@val-co.com to Safe Senders list"
echo "2. Check Message Trace in Office 365 Admin:"
echo "   - Go to Exchange Admin Center"
echo "   - Mail flow > Message trace"
echo "   - Search for messages from last hour"
echo