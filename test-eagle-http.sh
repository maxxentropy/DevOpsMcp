#!/bin/bash

# Test Eagle script execution via MCP protocol over HTTP

echo "Testing Eagle script execution via MCP HTTP endpoint..."

# Test 1: Simple variable test
echo -e "\nTest 1: Simple variable test"
curl -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
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
  }' | jq .

# Test 2: Variable injection
echo -e "\nTest 2: Variable injection"
curl -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
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
  }' | jq .

# Test 3: Arithmetic expression
echo -e "\nTest 3: Arithmetic expression"
curl -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
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
  }' | jq .

# Test 4: List all available tools
echo -e "\nListing all available MCP tools..."
curl -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "params": {},
    "id": 4
  }' | jq .