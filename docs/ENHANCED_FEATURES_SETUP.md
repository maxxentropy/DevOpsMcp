# Enhanced Features Setup Guide

## Overview

The DevOps MCP server now integrates directly with a shared PostgreSQL/Supabase database to provide enhanced features including task management, knowledge base with RAG, document versioning, and cross-platform project organization.

## Configuration

Add the following to your `appsettings.json` or environment variables:

```json
{
  "EnhancedFeatures": {
    "DatabaseUrl": "Host=db.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password",
    "SupabaseUrl": "https://your-project.supabase.co",
    "SupabaseKey": "your-supabase-key",
    "EnableVectorSearch": true,
    "MaxSearchResults": 10,
    "EmbeddingDimensions": 1536
  }
}
```

Or use environment variables:
```bash
export EnhancedFeatures__DatabaseUrl="Host=db.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=xxx"
export EnhancedFeatures__SupabaseUrl="https://xxx.supabase.co"
export EnhancedFeatures__SupabaseKey="xxx"
```

## Features Enabled

### 1. Task Management
- Create and manage tasks across platforms
- Task lifecycle: todo → doing → review → done
- Hierarchical tasks with subtasks
- Soft delete with archival
- Integration with Azure DevOps work items

### 2. Knowledge Base
- Vector search using pgvector
- Code example search
- Document chunking and embedding
- Source management

### 3. Document Versioning
- Automatic version snapshots
- Rollback capabilities
- Change tracking
- Audit trail

### 4. Enhanced Projects
- Cross-platform project management
- JSONB storage for flexible documents
- Feature planning
- Data modeling

## Database Schema

The integration uses the existing database schema with these main tables:
- `archon_projects` - Enhanced project management
- `archon_tasks` - Task tracking with full lifecycle
- `archon_crawled_pages` - Knowledge documents with embeddings
- `archon_document_versions` - Version control for documents
- `archon_sources` - Knowledge source management

## Entity Mapping

| Database Table | C# Entity | Purpose |
|----------------|-----------|----------|
| archon_projects | EnhancedProject | Cross-platform projects |
| archon_tasks | DevOpsTask | Task management |
| archon_crawled_pages | KnowledgeDocument | RAG knowledge base |
| archon_document_versions | DocumentVersion | Version control |
| archon_sources | KnowledgeSource | Knowledge organization |

## Next Steps

1. Configure database connection in appsettings.json
2. Implement MCP tools for task management
3. Add knowledge search capabilities
4. Create project management tools
5. Build document versioning tools

## Security Notes

- Use SSL connections for production databases
- Store credentials securely (Azure Key Vault recommended)
- Respect Row Level Security (RLS) policies
- Implement proper access controls