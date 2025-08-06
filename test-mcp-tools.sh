#!/bin/bash

# Test MCP tools via SSE endpoint

echo "Testing MCP Email Tools..."

# Function to send SSE request
send_sse_request() {
    local request=$1
    echo "Sending request: $request"
    
    (echo -e "POST /mcp HTTP/1.1\r\nHost: localhost:8080\r\nContent-Type: application/json\r\nContent-Length: ${#request}\r\n\r\n$request"; sleep 2) | nc localhost 8080 | grep -A 1000 "data:" | grep -E "data:|^$" | while IFS= read -r line; do
        if [[ $line == data:* ]]; then
            echo "${line:5}" | jq . 2>/dev/null || echo "${line:5}"
        fi
    done
}

# List all tools
echo -e "\n=== Listing all tools ==="
send_sse_request '{"jsonrpc":"2.0","method":"tools/list","id":1}'

# Get email-specific tools
echo -e "\n=== Getting email tool details ==="
send_sse_request '{"jsonrpc":"2.0","method":"tools/list","id":2}' | jq -r '.result.tools[] | select(.name | contains("email") or contains("send") or contains("quota") or contains("statistics")) | "\(.name): \(.description)"' 2>/dev/null || true