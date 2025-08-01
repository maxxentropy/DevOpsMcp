# DevOps MCP Server

A production-ready Model Context Protocol (MCP) server providing comprehensive Azure DevOps API integration with enterprise-grade architecture.

## Features

- **Complete Azure DevOps API Coverage**: Projects, work items, builds, repositories, pull requests, test plans, and artifacts
- **Multi-Protocol Support**: SSE (Server-Sent Events), Standard I/O, and HTTP streaming
- **Enterprise Security**: PAT, OAuth 2.0, and Azure AD authentication
- **Real-time Updates**: Webhook support for live status updates
- **Clean Architecture**: Domain-driven design with CQRS pattern
- **Production Ready**: Docker, Kubernetes, monitoring, and comprehensive testing

## Architecture

```
DevOpsMcp/
├── src/
│   ├── DevOpsMcp.Domain/          # Domain entities, value objects, interfaces
│   ├── DevOpsMcp.Application/     # Use cases, commands, queries, handlers
│   ├── DevOpsMcp.Infrastructure/  # Azure DevOps API clients, persistence
│   ├── DevOpsMcp.Server/         # MCP server implementation, protocols
│   └── DevOpsMcp.Contracts/      # DTOs, API contracts, external interfaces
└── tests/                        # Comprehensive test suite
```

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Azure DevOps account with Personal Access Token
- Docker (optional)
- Kubernetes cluster (optional)

### Running Locally

1. Clone the repository:
```bash
git clone https://github.com/devops-mcp/devops-mcp.git
cd devops-mcp
```

2. Configure settings:
```bash
cp src/DevOpsMcp.Server/appsettings.json src/DevOpsMcp.Server/appsettings.Development.json
# Edit appsettings.Development.json with your Azure DevOps credentials
```

3. Run the server:
```bash
dotnet run --project src/DevOpsMcp.Server
```

### Running with Docker

```bash
docker build -t devops-mcp .
docker run -e AzureDevOps__PersonalAccessToken=YOUR_PAT \
           -e AzureDevOps__OrganizationUrl=https://dev.azure.com/YOUR_ORG \
           -p 8080:8080 devops-mcp
```

### Running with Docker Compose

```bash
export AZURE_DEVOPS_ORG_URL=https://dev.azure.com/your-org
export AZURE_DEVOPS_PAT=your-pat
docker-compose up -d
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `MCP__Protocol` | Protocol mode: `stdio`, `sse`, `http` | `stdio` |
| `AzureDevOps__OrganizationUrl` | Your Azure DevOps organization URL | Required |
| `AzureDevOps__PersonalAccessToken` | Personal Access Token | Required |
| `AzureDevOps__AuthMethod` | Authentication method | `PersonalAccessToken` |
| `AzureDevOps__EnableCaching` | Enable response caching | `true` |

### Authentication Methods

1. **Personal Access Token (PAT)**
   - Create a PAT in Azure DevOps with appropriate scopes
   - Set `AzureDevOps__PersonalAccessToken` environment variable

2. **Azure AD**
   - Register an app in Azure AD
   - Configure client ID, secret, and tenant ID
   - Set `AzureDevOps__AuthMethod` to `AzureAD`

## Available Tools

### Project Management
- `list_projects` - Get all accessible projects
- `get_project_details` - Detailed project information
- `create_project` - Create new project
- `update_project_settings` - Modify project configuration

### Work Item Management
- `create_work_item` - Create new work items
- `update_work_item` - Modify existing work items
- `get_work_item` - Retrieve work item details
- `query_work_items` - WIQL query support
- `link_work_items` - Manage relationships
- `add_work_item_comment` - Add comments

### Build & Release
- `trigger_build` - Start new builds
- `get_build_status` - Real-time build monitoring
- `get_build_logs` - Retrieve build logs
- `create_release` - Deploy to environments
- `approve_release` - Approval workflow
- `get_deployment_status` - Track deployments

### Repository Management
- `list_repositories` - Get all repositories
- `create_pull_request` - Create PRs with work item linking
- `review_pull_request` - Add comments and approve
- `merge_pull_request` - Complete PR with policies
- `get_commit_history` - Repository change tracking

## Development

### Building from Source

```bash
dotnet build
dotnet test
dotnet publish -c Release
```

### Running Tests

```bash
# Unit tests
dotnet test tests/DevOpsMcp.Domain.Tests
dotnet test tests/DevOpsMcp.Application.Tests

# Integration tests
dotnet test tests/DevOpsMcp.Integration.Tests

# All tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Code Quality

```bash
# Run code analysis
dotnet format --verify-no-changes

# Security scanning
dotnet list package --vulnerable
```

## Deployment

### Kubernetes

```bash
# Apply base configuration
kubectl apply -k k8s/base

# Apply production overlay
kubectl apply -k k8s/overlays/prod

# Check deployment status
kubectl -n devops-mcp get pods
```

### Azure Container Apps

```bash
# Deploy to Azure Container Apps
az containerapp create \
  --name devops-mcp \
  --resource-group rg-devops-mcp \
  --environment devops-mcp-env \
  --image devops-mcp:latest \
  --target-port 8080 \
  --ingress 'external' \
  --min-replicas 1 \
  --max-replicas 10
```

## Monitoring

### Health Checks
- Liveness: `GET /health`
- Readiness: `GET /health/ready`

### Metrics
- Prometheus endpoint: `GET /metrics`
- Application Insights integration
- Custom metrics for Azure DevOps operations

### Logging
- Structured logging with Serilog
- Log levels: Debug, Information, Warning, Error
- Correlation IDs for request tracking

## Security

- **Input Validation**: All inputs sanitized and validated
- **Authentication**: Multiple auth methods supported
- **Authorization**: Per-operation permission checks
- **Secrets Management**: Azure Key Vault integration
- **Rate Limiting**: Configurable per-user limits
- **Audit Logging**: All operations tracked

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

MIT License - see [LICENSE](LICENSE) for details

## Support

- GitHub Issues: [github.com/devops-mcp/devops-mcp/issues](https://github.com/devops-mcp/devops-mcp/issues)
- Documentation: [docs.devops-mcp.io](https://docs.devops-mcp.io)
- Discord: [discord.gg/devops-mcp](https://discord.gg/devops-mcp)