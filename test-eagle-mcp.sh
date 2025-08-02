#!/bin/bash

# Test Eagle script execution via MCP protocol

echo "Testing Eagle script execution via MCP protocol..."

# Create a simple MCP request to execute an Eagle script
MCP_REQUEST='{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "execute_eagle_script",
    "arguments": {
      "script": "set greeting \"Hello from Eagle!\"; puts $greeting",
      "securityLevel": "Standard"
    }
  },
  "id": 1
}'

echo "Sending MCP request to execute Eagle script..."
echo "$MCP_REQUEST" | docker exec -i devops-mcp-server ./DevOpsMcp.Server

echo -e "\n\nTesting with variables..."
MCP_REQUEST_VARS='{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "execute_eagle_script",
    "arguments": {
      "script": "puts \"The value of x is: $x\"",
      "variablesJson": "{\"x\": 42}",
      "securityLevel": "Standard"
    }
  },
  "id": 2
}'

echo "$MCP_REQUEST_VARS" | docker exec -i devops-mcp-server ./DevOpsMcp.Server

echo -e "\n\nTesting arithmetic..."
MCP_REQUEST_MATH='{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "execute_eagle_script",
    "arguments": {
      "script": "expr 2 + 2",
      "securityLevel": "Standard"
    }
  },
  "id": 3
}'

echo "$MCP_REQUEST_MATH" | docker exec -i devops-mcp-server ./DevOpsMcp.Server