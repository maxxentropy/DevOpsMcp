# Running the DevOps MCP Server

This guide explains how to run the DevOps MCP Server with the Streamable HTTP transport protocol for integration with Claude Code and other MCP clients.

## Prerequisites

- Docker and Docker Compose installed
- Azure DevOps Personal Access Token (PAT)
- Azure DevOps Organization URL

## Environment Setup

Create a `.env` file in the project root with your Azure DevOps credentials:

```bash
AZURE_DEVOPS_ORG_URL=https://dev.azure.com/your-organization
AZURE_DEVOPS_PAT=your-personal-access-token
```

**Security Note**: Never commit the `.env` file to version control. It's already included in `.gitignore`.

## Running with Docker

### Option 1: Simple Docker Compose (Recommended)

This runs just the MCP server without the full monitoring stack:

```bash
docker-compose -f docker-compose.simple.yml up
```

The server will be available at `http://localhost:8080/mcp`

### Option 2: Full Stack with Monitoring

This includes Prometheus, Grafana, and Redis:

```bash
docker-compose up
```

Services:
- MCP Server: `http://localhost:8080/mcp`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`
- Redis: `localhost:6379`

### Option 3: Direct Docker Run

```bash
# Build the image
docker build -t devops-mcp .

# Run the container
docker run -p 8080:8080 \
  -e AZURE_DEVOPS_ORG_URL="$AZURE_DEVOPS_ORG_URL" \
  -e AZURE_DEVOPS_PAT="$AZURE_DEVOPS_PAT" \
  devops-mcp
```

## Testing the Server

### 1. Health Check

```bash
curl http://localhost:8080/health
```

### 2. Test MCP Endpoint

Send a test request to initialize the connection:

```bash
curl -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "initialize",
    "params": {
      "clientInfo": {
        "name": "test-client",
        "version": "1.0.0"
      }
    },
    "id": 1
  }'
```

### 3. List Available Tools

After initialization, list the available MCP tools:

```bash
curl -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -H "Mcp-Session-Id: test-session" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "params": {},
    "id": 2
  }'
```

## Claude Code Integration

### 1. Configure Claude Code

The repository includes a `.mcp.config.json` file that configures Claude Code to use this server:

```json
{
  "mcpServers": {
    "devops-mcp": {
      "type": "streamable-http",
      "url": "http://localhost:8080/mcp",
      "headers": {
        "Authorization": "Bearer ${AZURE_DEVOPS_PAT}"
      }
    }
  }
}
```

### 2. Using with Claude Code

1. Ensure the server is running (see above)
2. Open Claude Code in your project directory
3. The MCP server will be automatically detected and connected
4. You can now use Azure DevOps commands through Claude

Example commands:
- "List all work items in the current sprint"
- "Show me the build status for the main branch"
- "Create a new bug for authentication issues"

## Available MCP Tools

The server provides the following tools through MCP:

### Azure DevOps Tools
- `azure-devops-list-work-items` - List and filter work items
- `azure-devops-get-work-item` - Get details of a specific work item
- `azure-devops-create-work-item` - Create new work items
- `azure-devops-update-work-item` - Update existing work items
- `azure-devops-list-builds` - List build pipelines and their status
- `azure-devops-list-repositories` - List Git repositories
- `azure-devops-list-pull-requests` - List pull requests

### AI Persona Tools
- `select-persona` - Choose an AI persona (DevOps Engineer, SRE, etc.)
- `interact-with-persona` - Get advice from the selected persona
- `configure-persona-behavior` - Adjust persona response style
- `manage-persona-memory` - Control persona learning and memory

## Troubleshooting

### Connection Refused

If you get a connection refused error:
1. Check if the container is running: `docker ps`
2. Verify the port mapping: should show `0.0.0.0:8080->8080/tcp`
3. Check container logs: `docker logs devops-mcp-server`

### Authentication Errors

If you see authentication errors:
1. Verify your PAT is valid and has the necessary permissions
2. Check the PAT hasn't expired
3. Ensure the organization URL is correct

### CORS Issues

The server has CORS enabled by default. If you still encounter issues:
1. Check the browser console for specific CORS errors
2. Verify the client is sending proper headers
3. For development, you can use browser extensions to disable CORS

### Debugging

Enable debug logging by setting:
```bash
-e Logging__LogLevel__Default=Debug
```

View logs:
```bash
docker logs -f devops-mcp-server
```

## Development Mode

For local development without Docker:

```bash
# Set environment variables
export AZURE_DEVOPS_ORG_URL="https://dev.azure.com/your-org"
export AZURE_DEVOPS_PAT="your-pat"

# Run the server
cd src/DevOpsMcp.Server
dotnet run
```

The server will start on `http://localhost:5000` by default.

## Security Considerations

1. **PAT Storage**: Store your PAT securely, never in code
2. **Network Security**: In production, use HTTPS and proper authentication
3. **CORS**: Configure CORS policies appropriately for your environment
4. **Rate Limiting**: The server includes basic rate limiting for API calls
5. **Audit Logging**: All MCP operations are logged for security auditing

## Performance Tuning

For high-load scenarios:

1. **Increase Memory**: Add to docker-compose.yml:
   ```yaml
   deploy:
     resources:
       limits:
         memory: 2G
   ```

2. **Connection Pooling**: The server uses connection pooling by default

3. **Response Caching**: Work item queries are cached for 5 minutes

## Next Steps

- Review the [API Documentation](./api-reference.md) for detailed tool usage
- Check [Persona Guide](./persona-system.md) for AI persona features
- See [Azure DevOps Integration](./azure-devops-setup.md) for PAT permissions