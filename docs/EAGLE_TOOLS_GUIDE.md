# Eagle Tools Guide

## Overview

This guide documents the Eagle scripting tools available in the DevOps MCP Server, including their usage, parameters, and examples.

## Available Eagle Tools

### 1. execute_eagle_script

**Purpose**: Execute Eagle/Tcl scripts in a secure sandbox with full MCP integration.

**Parameters**:
- `script` (required): The Eagle script code to execute
- `variables` (optional): JSON object containing variables to inject into the script
- `sessionId` (optional): Session identifier for persistent state across executions
- `securityLevel` (optional): Security level - "Minimal", "Standard", "Elevated", "Maximum" (default: "Standard")

**Example Usage**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "execute_eagle_script",
    "arguments": {
      "script": "set user [mcp::context get user.name]\nputs \"Hello, $user!\"",
      "sessionId": "session-123",
      "securityLevel": "Standard"
    }
  }
}
```

**Features**:
- Rich context injection via `mcp::context` commands
- Session persistence via `mcp::session` commands
- Tool integration via `mcp::call_tool` command
- Structured output support (auto-detects JSON, Tcl dicts/lists)
- Security sandboxing with command restrictions
- Execution metrics and timeout enforcement

### 2. eagle_history

**Purpose**: Query Eagle script execution history with filtering options.

**Parameters**:
- `sessionId` (optional): Filter history by specific session ID
- `limit` (optional): Maximum number of entries to return (default: 10)
- `detailed` (optional): Include detailed metrics and results (default: false)

**Example Usage**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "eagle_history",
    "arguments": {
      "sessionId": "session-123",
      "limit": 20,
      "detailed": true
    }
  }
}
```

**Output**:
- Summary mode: Compact list with status indicators, timestamps, and duration
- Detailed mode: Full execution details including metrics, results, and error messages

## Eagle Script Commands

### Context Commands

**mcp::context get <path>**
- Retrieves values from the DevOps context
- Paths: `user.name`, `user.role`, `project.name`, `project.id`, `organization.name`, `environment.type`, `environment.isProduction`

Example:
```tcl
set projectName [mcp::context get project.name]
set isProd [mcp::context get environment.isProduction]
if {$isProd eq "true"} {
    puts "Running in production!"
}
```

### Session Commands

**mcp::session set <key> <value>**
- Stores a value in the persistent session

**mcp::session get <key>**
- Retrieves a value from the session

**mcp::session list**
- Returns all session keys

**mcp::session clear**
- Removes all session values

Example:
```tcl
# Store deployment info
mcp::session set "lastDeployment" "deploy-123"
mcp::session set "deploymentTime" [clock seconds]

# Retrieve later
set lastDeploy [mcp::session get "lastDeployment"]
puts "Last deployment: $lastDeploy"
```

### Tool Integration Commands

**mcp::call_tool <toolName> <arguments>**
- Calls another MCP tool from within the script
- Arguments should be a Tcl dictionary

Example:
```tcl
# Call the list_projects tool
set projects [mcp::call_tool "list_projects" {}]

# Call query_work_items with parameters
set workItems [mcp::call_tool "query_work_items" [dict create \
    projectId "MyProject" \
    wiql "SELECT [System.Id] FROM WorkItems WHERE [System.State] = 'Active'"
]]
```

### Output Formatting Commands

**mcp::output <data> <format>**
- Formats data for structured output
- Formats: json, xml, yaml, table, csv, markdown

Example:
```tcl
set data [dict create \
    name "Build Status" \
    builds [list \
        [dict create id 1 status "Success"] \
        [dict create id 2 status "Failed"] \
    ]
]

# Output as JSON
puts [mcp::output $data json]

# Output as table
puts [mcp::output $data table]
```

## Security Levels

### Minimal
- No file system access
- No network operations
- No process execution
- No CLR reflection
- Basic Tcl commands only

### Standard (Default)
- Limited file system access (read-only, specific paths)
- No network operations
- No process execution
- Limited CLR reflection
- Most Tcl commands available

### Elevated
- Full file system access within allowed paths
- No network operations
- CLR reflection allowed
- No process execution
- Extended execution time limits

### Maximum
- Full access to all Eagle/Tcl features
- Network operations allowed
- Process execution allowed
- No command restrictions
- Extended resource limits

## Best Practices

### 1. Error Handling
```tcl
if {[catch {
    set result [mcp::call_tool "some_tool" $args]
} error]} {
    puts "Error calling tool: $error"
    return [dict create status "error" message $error]
}
```

### 2. Structured Output
```tcl
# Always return structured data for better integration
return [dict create \
    status "success" \
    data $result \
    metadata [dict create \
        timestamp [clock seconds] \
        user [mcp::context get user.name] \
    ]
]
```

### 3. Session Management
```tcl
# Clean up session when done with temporary data
mcp::session set "temp_data" $data
# ... use the data ...
mcp::session set "temp_data" ""  ;# Clear when done
```

### 4. Security Considerations
- Always validate input parameters
- Use appropriate security level for the task
- Avoid storing sensitive data in sessions
- Be cautious with file system operations

## Advanced Examples

### Multi-Tool Workflow
```tcl
# Get active bugs and create summary report
set projectId [mcp::context get project.id]

# Query for bugs
set bugQuery "SELECT [System.Id], [System.Title] FROM WorkItems WHERE [System.WorkItemType] = 'Bug' AND [System.State] = 'Active'"
set bugs [mcp::call_tool "query_work_items" [dict create projectId $projectId wiql $bugQuery]]

# Process results
set bugCount [llength $bugs]
set summary [dict create \
    project $projectId \
    activeBugs $bugCount \
    timestamp [clock format [clock seconds]] \
]

# Store in session for later
mcp::session set "bug_summary" $summary

# Return formatted output
return [mcp::output $summary json]
```

### Conditional Logic Based on Environment
```tcl
set envType [mcp::context get environment.type]
set isProd [mcp::context get environment.isProduction]

if {$isProd eq "true"} {
    puts "Production environment - using careful deployment"
    set deploymentStrategy "blue-green"
} else {
    puts "Non-production environment - using direct deployment"
    set deploymentStrategy "direct"
}

# Store strategy for other scripts
mcp::session set "deployment_strategy" $deploymentStrategy
```

## Troubleshooting

### Common Issues

1. **Session not persisting**: Ensure you're using the same sessionId across calls
2. **Command not found**: Check the security level - some commands are restricted
3. **Tool call failing**: Verify the tool name and argument format
4. **Output not structured**: Use `mcp::output` or return Tcl dictionaries

### Debugging Tips

1. Use `puts` for debugging output
2. Check execution history with `eagle_history` tool
3. Verify security level matches requirements
4. Test scripts with minimal security first, then increase as needed

## Future Enhancements (Phase 2+)

- Dynamic tool registration from Eagle scripts
- Event-driven background scripts
- Script marketplace integration
- Advanced debugging capabilities
- Performance optimization features