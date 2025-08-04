#!/bin/bash

echo "Testing connection to MCP server..."
echo

# Test 1: Basic connectivity
echo "1. Testing basic connectivity to localhost:8080..."
if nc -zv localhost 8080 2>&1 | grep -q "succeeded"; then
    echo "✓ Port 8080 is open"
else
    echo "✗ Port 8080 is not accessible"
fi
echo

# Test 2: HTTP request with curl
echo "2. Testing HTTP request to /mcp endpoint..."
echo "Command: curl -v --connect-timeout 5 http://localhost:8080/mcp"
curl -v --connect-timeout 5 http://localhost:8080/mcp
echo
echo

# Test 3: Simple Python test
echo "3. Testing with Python..."
python3 << 'EOF'
import urllib.request
import json

try:
    # Try a simple request
    req = urllib.request.Request(
        'http://localhost:8080/mcp',
        data=json.dumps({"jsonrpc": "2.0", "id": 1, "method": "test"}).encode('utf-8'),
        headers={'Content-Type': 'application/json'}
    )
    response = urllib.request.urlopen(req, timeout=5)
    print("✓ Python can connect to the server")
    print(f"Response status: {response.status}")
except Exception as e:
    print(f"✗ Python connection failed: {e}")
EOF
echo

# Test 4: Check Docker container
echo "4. Checking Docker container..."
if docker ps | grep -q "devops-mcp"; then
    echo "✓ devops-mcp container is running"
    echo
    echo "Container details:"
    docker ps | grep devops-mcp
else
    echo "✗ devops-mcp container not found in running containers"
    echo
    echo "All running containers:"
    docker ps
fi
echo

echo "Connection test complete."