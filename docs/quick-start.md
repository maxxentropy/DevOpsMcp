# Quick Start Guide

Get the DevOps MCP Server running in 5 minutes!

## 1. Clone the Repository

```bash
git clone https://github.com/maxxentropy/DevOpsMcp.git
cd DevOpsMcp
```

## 2. Set Environment Variables

Create a `.env` file:

```bash
AZURE_DEVOPS_ORG_URL=https://dev.azure.com/your-organization
AZURE_DEVOPS_PAT=your-personal-access-token
```

## 3. Start the Server

```bash
docker-compose -f docker-compose.simple.yml up
```

## 4. Test the Connection

```bash
curl -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "initialize",
    "params": {
      "clientInfo": {
        "name": "test",
        "version": "1.0"
      }
    },
    "id": 1
  }'
```

## 5. Use with Claude Code

The server is automatically configured for Claude Code. Just open Claude Code in your project directory and start using Azure DevOps commands!

## Example Commands

- "Show me all active bugs"
- "List recent build failures"
- "Create a task for updating documentation"
- "Get pull requests waiting for review"

## What's Next?

- [Full Setup Guide](./running-the-server.md) - Detailed configuration options
- [Tool Reference](./tool-reference.md) - Complete list of available tools
- [Troubleshooting](./troubleshooting.md) - Common issues and solutions