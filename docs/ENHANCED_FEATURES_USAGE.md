# Enhanced Features Usage Guide

## Overview

The DevOps MCP server now includes enhanced features that integrate directly with a shared PostgreSQL/Supabase database. These features provide task management, knowledge base search, and project organization capabilities.

## Configuration Required

Before using enhanced features, configure the database connection in your `appsettings.json`:

```json
{
  "EnhancedFeatures": {
    "DatabaseUrl": "Host=db.xxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password",
    "SupabaseUrl": "https://xxx.supabase.co",
    "SupabaseKey": "your-supabase-service-key"
  }
}
```

## Available MCP Tools

### Task Management

#### create_task
Create a new task in the system.

```json
{
  "tool": "create_task",
  "arguments": {
    "projectId": "550e8400-e29b-41d4-a716-446655440000",
    "title": "Implement OAuth2 authentication",
    "description": "Add Google and GitHub OAuth2 providers",
    "status": "todo",
    "assignee": "AI IDE Agent",
    "taskOrder": 10,
    "feature": "authentication"
  }
}
```

#### list_tasks
List tasks with filtering and pagination.

```json
{
  "tool": "list_tasks",
  "arguments": {
    "projectId": "550e8400-e29b-41d4-a716-446655440000",
    "status": "todo",
    "assignee": "AI IDE Agent",
    "sortBy": "priority",
    "take": 25
  }
}
```

#### update_task
Update an existing task.

```json
{
  "tool": "update_task",
  "arguments": {
    "taskId": "task-123e4567-e89b-12d3-a456-426614174000",
    "status": "doing",
    "description": "Updated description with more details"
  }
}
```

### Project Management

#### manage_project
Unified tool for project operations (create, list, get, update, delete).

Create a project:
```json
{
  "tool": "manage_project",
  "arguments": {
    "action": "create",
    "title": "E-commerce Platform v2",
    "description": "Next generation e-commerce platform",
    "githubRepo": "https://github.com/company/ecommerce-v2"
  }
}
```

List all projects:
```json
{
  "tool": "manage_project",
  "arguments": {
    "action": "list"
  }
}
```

Get specific project:
```json
{
  "tool": "manage_project",
  "arguments": {
    "action": "get",
    "projectId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### Knowledge Search

#### search_knowledge
Search the knowledge base using semantic search (requires embedding service).

```json
{
  "tool": "search_knowledge",
  "arguments": {
    "query": "OAuth2 implementation best practices",
    "searchType": "documents",
    "matchCount": 10
  }
}
```

Note: Full semantic search requires an embedding service (e.g., OpenAI) to be configured for generating vectors.

## Task Status Workflow

Tasks follow this lifecycle:
- `todo` - Task is ready to be started
- `doing` - Task is actively being worked on
- `review` - Task implementation complete, awaiting validation
- `done` - Task validated and integrated

## Features

### 1. Cross-Platform Integration
- Tasks created in DevOps MCP are visible in other systems using the same database
- Real-time updates without synchronization delays
- Unified project view across platforms

### 2. Hierarchical Tasks
- Support for parent-child task relationships
- Automatic subtask archival when parent is archived
- Task ordering for priority management

### 3. Flexible Metadata
- JSONB storage for sources and code examples
- Custom feature grouping
- Extensible without schema changes

### 4. Soft Delete
- Tasks are archived rather than deleted
- Full audit trail maintained
- Can be restored if needed

## Best Practices

1. **Task Creation**
   - Use descriptive titles
   - Include acceptance criteria in descriptions
   - Set appropriate task order for prioritization
   - Link to relevant features

2. **Project Organization**
   - Use projects to group related work
   - Pin important projects for visibility
   - Store flexible data in JSONB fields

3. **Status Management**
   - Update task status as work progresses
   - Use "review" status for code review
   - Archive completed tasks periodically

## Troubleshooting

### Connection Issues
- Verify database URL format
- Check network connectivity to Supabase
- Ensure credentials are correct
- Look for connection errors in logs

### Tool Registration
- Enhanced tools only register if database is configured
- Check logs for "Registering Enhanced Feature tools" message
- Verify EnhancedFeatures configuration section exists

### Performance
- Use filtering to reduce result sets
- Implement pagination for large lists
- Consider indexing frequently queried fields