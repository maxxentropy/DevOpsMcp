#!/bin/bash

# Test Eagle script execution

echo "Testing Eagle script execution..."

# Simple test - set and get a variable
echo "Test 1: Simple variable test"
curl -X POST http://localhost:8080/mcp/tools/execute_eagle_script \
  -H "Content-Type: application/json" \
  -d '{
    "script": "set greeting \"Hello from Eagle!\"; puts $greeting",
    "securityLevel": "Standard"
  }' -v

echo -e "\n\nTesting with variables..."

# Test with variables
echo "Test 2: Variable injection"
curl -X POST http://localhost:8080/mcp/tools/execute_eagle_script \
  -H "Content-Type: application/json" \
  -d '{
    "script": "puts \"The value of x is: $x\"",
    "variablesJson": "{\"x\": 42}",
    "securityLevel": "Standard"
  }' -v

echo -e "\n\nTesting arithmetic..."

# Test arithmetic
echo "Test 3: Arithmetic expression"
curl -X POST http://localhost:8080/mcp/tools/execute_eagle_script \
  -H "Content-Type: application/json" \
  -d '{
    "script": "expr 2 + 2",
    "securityLevel": "Standard"
  }' -v