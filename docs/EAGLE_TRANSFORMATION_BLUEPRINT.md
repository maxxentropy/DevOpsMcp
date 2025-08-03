# Eagle-Powered MCP Server: Complete Transformation Blueprint

## Executive Summary

This blueprint transforms your MCP server from basic Eagle script execution into a revolutionary automation platform where **Eagle scripting is the primary interface** for all DevOps operations. Eagle evolves through four phases from a simple tool to the core engine that powers dynamic tools, conversational automation, and intelligent infrastructure management.

**Key Principle**: Eagle scripting integration is the foundation that enables all advanced DevOps capabilities. Every feature builds upon Eagle's extensibility.

## Current State → Target State

### Starting Point
- Basic MCP server with `execute_eagle_script` tool
- Simple parameter injection into Eagle scripts
- Text-based script output

### Target State
- Eagle scripts **are** MCP tools (not just executed by them)
- Eagle scripts **are** conversational interfaces (new `scripts` modality)
- Eagle scripts **are** autonomous agents (event-driven background services)
- All DevOps capabilities flow through Eagle scripting

## Phase 1: Enhanced Eagle Core (Weeks 1-4)

### 1.1 Rich Context Injection (Week 1-2)

**Goal**: Replace simple variable injection with comprehensive context access.

**Implementation**:
```csharp
// Add to your existing MCP server
public class EagleContextProvider
{
    public void InjectRichContext(IEagleInterpreter interpreter, DevOpsContext devOpsContext)
    {
        // Create mcp:: namespace commands
        interpreter.AddCommand("mcp::context", new ContextCommand(devOpsContext));
        interpreter.AddCommand("mcp::session", new SessionCommand());
        interpreter.AddCommand("mcp::call_tool", new ToolCallCommand(this.mcpServer));
    }
}

public class ContextCommand : IEagleCommand
{
    public ReturnCode Execute(Interpreter interpreter, IClientData clientData, 
        ArgumentList arguments, ref Result result)
    {
        if (arguments.Count != 3) 
        {
            result = "Usage: mcp::context get key.path";
            return ReturnCode.Error;
        }
        
        var action = arguments[1].ToString();
        var keyPath = arguments[2].ToString();
        
        if (action == "get")
        {
            result = GetContextValue(keyPath);
            return ReturnCode.Ok;
        }
        
        result = "Unknown context action";
        return ReturnCode.Error;
    }
}
```

**Eagle Script Interface**:
```tcl
# All Eagle scripts now have access to:
set userName [mcp::context get user.name]
set projectId [mcp::context get project.id]
set buildStatus [mcp::context get project.lastBuild.status]
set envType [mcp::context get environment.type]

# Session state persists across script calls
mcp::session set "lastDeployment" $deploymentId
set lastDeploy [mcp::session get "lastDeployment"]

# Call other MCP tools from within scripts
set workItems [mcp::call_tool "query_work_items" [dict create projectId $projectId]]
```

**Success Criteria**:
- Eagle scripts can access full DevOps context without parameters
- Session state persists between script executions
- Scripts can call other MCP tools seamlessly

### 1.2 Structured Output Processing (Week 3-4)

**Goal**: Eagle scripts return structured data, not just text.

**Implementation**:
```csharp
public class EagleOutputProcessor
{
    public object ProcessOutput(string rawOutput)
    {
        // Detect Tcl dictionary format
        if (rawOutput.Trim().StartsWith("dict create") || IsTclDict(rawOutput))
        {
            return ConvertTclDictToJson(rawOutput);
        }
        
        // Detect JSON format
        if (rawOutput.Trim().StartsWith("{") || rawOutput.Trim().StartsWith("["))
        {
            return JsonSerializer.Deserialize<object>(rawOutput);
        }
        
        // Return as text content
        return new { content = rawOutput, type = "text" };
    }
}
```

**Eagle Script Output**:
```tcl
# Scripts can now return structured data
set result [dict create \
    status "success" \
    deployments [list \
        [dict create id 1 environment "staging" status "active"] \
        [dict create id 2 environment "prod" status "pending"] \
    ] \
    summary [dict create total 2 active 1] \
]

return $result
```

**Success Criteria**:
- Eagle scripts return JSON objects instead of plain text
- MCP responses have structured data in content field
- Backward compatibility with text-only scripts

## Phase 2: Eagle Scripts as MCP Tools (Weeks 5-8)

### 2.1 Dynamic Tool Registration (Week 5-6)

**Goal**: Automate the registration of Eagle scripts as MCP tools, allowing for hot-reloading and dynamic extension of the server's capabilities.

**Implementation**: Create a new service that monitors a directory and registers/unregisters tools with the main IToolRegistry.

```csharp
// Create new file: /src/DevOpsMcp.Server/Tools/Eagle/EagleToolProvider.cs
public class EagleToolProvider : IHostedService
{
    private readonly FileSystemWatcher _watcher;
    private readonly IToolRegistry _toolRegistry;
    private readonly IMediator _mediator; // To execute the script
    private readonly ILogger<EagleToolProvider> _logger;

    public EagleToolProvider(IToolRegistry toolRegistry, IMediator mediator, ILogger<EagleToolProvider> logger)
    {
        _toolRegistry = toolRegistry;
        _mediator = mediator;
        _logger = logger;
        _watcher = new FileSystemWatcher("./eagle-tools", "*.eagle");
        _watcher.Created += (s, e) => RegisterToolFromFile(e.FullPath);
        _watcher.Changed += (s, e) => RegisterToolFromFile(e.FullPath);
        _watcher.Deleted += (s, e) => UnregisterTool(Path.GetFileNameWithoutExtension(e.Name));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Ensure directory exists
        Directory.CreateDirectory("./eagle-tools");
        
        // Initial scan
        foreach (var file in Directory.GetFiles("./eagle-tools", "*.eagle"))
        {
            RegisterToolFromFile(file);
        }
        _watcher.EnableRaisingEvents = true;
        return Task.CompletedTask;
    }
    
    private void RegisterToolFromFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var metadata = ParseEagleToolMetadata(content);
            
            if (metadata != null)
            {
                var eagleTool = new EagleTool(metadata, content, _mediator);
                _toolRegistry.RegisterTool(eagleTool);
                _logger.LogInformation("Registered Eagle tool: {ToolName}", metadata.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register Eagle tool from {FilePath}", filePath);
        }
    }
    
    private EagleToolMetadata ParseEagleToolMetadata(string content)
    {
        var lines = content.Split('\n');
        var metadata = new EagleToolMetadata();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("# MCP-Tool-Name:"))
                metadata.Name = trimmedLine.Substring(16).Trim();
            else if (trimmedLine.StartsWith("# MCP-Tool-Description:"))
                metadata.Description = trimmedLine.Substring(23).Trim().Trim('"');
            else if (trimmedLine.StartsWith("# MCP-Tool-InputSchema:"))
            {
                var schemaJson = trimmedLine.Substring(23).Trim();
                metadata.InputSchema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
            }
        }
        
        return string.IsNullOrEmpty(metadata.Name) ? null : metadata;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.Dispose();
        return Task.CompletedTask;
    }
}

// Create new file: /src/DevOpsMcp.Server/Tools/Eagle/EagleTool.cs
public class EagleTool : ITool
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public JsonElement InputSchema { get; private set; }
    private readonly string _scriptContent;
    private readonly IMediator _mediator;

    public EagleTool(EagleToolMetadata metadata, string scriptContent, IMediator mediator)
    {
        Name = metadata.Name;
        Description = metadata.Description;
        InputSchema = metadata.InputSchema;
        _scriptContent = scriptContent;
        _mediator = mediator;
    }

    public async Task<CallToolResponse> ExecuteAsync(JsonElement? arguments, CancellationToken cancellationToken)
    {
        var command = new ExecuteEagleScriptCommand
        {
            Script = _scriptContent,
            VariablesJson = arguments?.GetRawText(),
            SessionId = Guid.NewGuid().ToString() // Or get from context
        };
        
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.IsSuccess)
        {
            return new CallToolResponse
            {
                Content = new List<ToolContent>
                {
                    new TextContent { Text = result.Value.Result }
                }
            };
        }
        else
        {
            return new CallToolResponse
            {
                IsError = true,
                Content = new List<ToolContent>
                {
                    new TextContent { Text = result.Value.ErrorMessage }
                }
            };
        }
    }
}

// Create new file: /src/DevOpsMcp.Server/Tools/Eagle/EagleToolMetadata.cs
public class EagleToolMetadata
{
    public string Name { get; set; }
    public string Description { get; set; }
    public JsonElement InputSchema { get; set; }
}
```

**Eagle Tool Definition Format** (`/eagle-tools/get_active_bugs.eagle`):
```tcl
# MCP-Tool-Name: get_active_bugs
# MCP-Tool-Description: "Retrieves active bugs for a project."
# MCP-Tool-InputSchema: { "type": "object", "properties": { "projectId": {"type": "string"} }, "required": ["projectId"] }

# The 'args' variable is automatically injected with the parsed input dictionary
set projectId [dict get $args projectId]

set query "SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Bug' AND [System.State] = 'Active'"
set result [mcp::call_tool "query_work_items" [dict create projectId $projectId wiql $query]]
return $result
```

**Success Criteria**:
- ✅ Dropping a new .eagle file into the /eagle-tools directory makes it available via tools/list within 5 seconds
- ✅ Updating the metadata comments in an .eagle file updates the tool's description and schema on the next tools/list call
- ✅ The dynamically registered tool can successfully receive arguments, call other tools, and return structured data

### 2.2 Core Eagle Tool Suite (Week 7-8)

**Goal**: Build a suite of 12 production-ready Eagle MCP tools that cover the core DevOps lifecycle.

**Required Tools** (based on existing domain interfaces):

1. **get_active_bugs.eagle** - Queries work items
2. **deploy_application.eagle** - Interacts with build and release systems  
3. **run_test_suite.eagle** - Triggers a build/test pipeline
4. **monitor_performance.eagle** - Queries monitoring data
5. **manage_environments.eagle** - Environment management
6. **code_quality_check.eagle** - Triggers build with analysis steps
7. **backup_database.eagle** - Database operations
8. **get_pr_status.eagle** - Calls IPullRequestService
9. **generate_weekly_report.eagle** - Aggregates data from multiple tool calls
10. **manage_secrets.eagle** - Secret management operations  
11. **configure_alerts.eagle** - Alerting configuration
12. **troubleshoot_build.eagle** - Fetches logs and artifacts for failed builds

**Example Tool Template** (`get_pr_status.eagle`):
```tcl
# MCP-Tool-Name: get_pr_status
# MCP-Tool-Description: "Gets the status of open pull requests for a repository."
# MCP-Tool-InputSchema: { "type": "object", "properties": { "repositoryId": {"type": "string"} }, "required": ["repositoryId"] }

# 1. Input validation
if {![dict exists $args repositoryId]} {
    error "Missing required parameter: repositoryId"
}
set repositoryId [dict get $args repositoryId]

# 2. Context access
set projectId [mcp::context get project.id]

# 3. Core logic using other tools
set prs [mcp::call_tool "get_pull_requests" [dict create projectId $projectId repositoryId $repositoryId status "active"]]

# 4. Structured output
set result [dict create \
    status "success" \
    data $prs \
    metadata [dict create timestamp [clock seconds] user [mcp::context get user.name]] \
]

return [to_json $result]
```

**Success Criteria**:
- ✅ All 12 tools are implemented and functional
- ✅ Each tool includes input validation, context access, core logic, and structured output
- ✅ The tool suite demonstrates key Eagle capabilities and serves as robust examples for community contributions

## Phase 3: Scripts as First-Class MCP Modality (Weeks 9-12)

### 3.1 Scripts Modality Implementation (Week 9-10)

**Goal**: Implement a new `scripts/execute` MCP method for stateful, session-based script execution.

**Implementation**: Extend the `MessageHandler` and create a session manager.

```csharp
// In: /src/DevOpsMcp.Server/Mcp/MessageHandler.cs
// Add a new case to the HandleRequestAsync switch statement
public async Task<McpResponse> HandleRequestAsync(McpRequest request, CancellationToken cancellationToken = default)
{
    // ... existing code ...
    var result = request.Method switch
    {
        "initialize" => await HandleInitializeAsync(request),
        "tools/list" => await HandleListToolsAsync(),
        "tools/call" => await HandleCallToolAsync(request, cancellationToken),
        // NEW MODALITY
        "scripts/execute" => await _scriptsModality.HandleScriptsExecute(request, cancellationToken), 
        _ => throw new NotSupportedException($"Method {request.Method} is not supported")
    };
    // ... existing code ...
}
```

```csharp
// Create new file: /src/DevOpsMcp.Server/Mcp/ScriptsModality.cs
public class ScriptsModality
{
    private readonly ConcurrentDictionary<string, EagleSession> _sessions = new();
    private readonly IEagleScriptExecutor _scriptExecutor;
    private readonly ILogger<ScriptsModality> _logger;
    
    public ScriptsModality(IEagleScriptExecutor scriptExecutor, ILogger<ScriptsModality> logger)
    {
        _scriptExecutor = scriptExecutor;
        _logger = logger;
    }
    
    public async Task<object> HandleScriptsExecute(McpRequest request, CancellationToken ct)
    {
        var requestData = JsonSerializer.Deserialize<ScriptsExecuteRequest>(request.Params.GetRawText());
        
        var sessionId = requestData.SessionId ?? Guid.NewGuid().ToString();
        
        // Get or create session
        var session = _sessions.GetOrAdd(sessionId, id => new EagleSession(id, _scriptExecutor));
        
        if (requestData.Streaming)
        {
            return await ExecuteStreamingScript(session, requestData.Script, ct);
        }
        else
        {
            return await ExecuteScript(session, requestData.Script, ct);
        }
    }
    
    private async Task<object> ExecuteScript(EagleSession session, string script, CancellationToken ct)
    {
        var result = await session.ExecuteAsync(script, ct);
        
        return new
        {
            sessionId = session.Id,
            result = result.Result,
            isSuccess = result.IsSuccess,
            error = result.ErrorMessage
        };
    }
    
    private async Task<object> ExecuteStreamingScript(EagleSession session, string script, CancellationToken ct)
    {
        // Implementation for streaming execution
        // This would require additional infrastructure for real-time communication
        throw new NotImplementedException("Streaming execution will be implemented in future iteration");
    }
}

// Supporting classes
public class ScriptsExecuteRequest
{
    public string SessionId { get; set; }
    public string Script { get; set; }
    public bool Streaming { get; set; } = false;
    public bool Persistent { get; set; } = true;
}
```

```csharp
// Create new file: /src/DevOpsMcp.Infrastructure/Eagle/EagleSession.cs
public class EagleSession
{
    public string Id { get; }
    public Interpreter Interpreter { get; private set; }
    public ConcurrentDictionary<string, object> State { get; } = new();
    private readonly IEagleScriptExecutor _scriptExecutor;
    
    public EagleSession(string id, IEagleScriptExecutor scriptExecutor)
    {
        Id = id;
        _scriptExecutor = scriptExecutor;
        InitializeSession();
    }
    
    private void InitializeSession()
    {
        // Create a persistent interpreter for this session
        Interpreter = Interpreter.Create();
        
        // Inject session-aware commands
        Interpreter.CreateCommand("mcp::session", new SessionCommand(this), (IClientData)null);
        // ... other command injections ...
    }
    
    public async Task<ExecutionResult> ExecuteAsync(string script, CancellationToken cancellationToken)
    {
        // Execute script within the persistent session interpreter
        var context = new DevOpsMcp.Domain.Eagle.ExecutionContext
        {
            SessionId = Id,
            Script = script
            // ... other context properties ...
        };
        
        return await _scriptExecutor.ExecuteScriptAsync(context, Interpreter);
    }
}
```

**Client Usage Example**:
```javascript
// Traditional tool call
const bugResult = await mcp.callTool("get_active_bugs", { projectId: "proj1" });

// New scripts modality - persistent session
const sessionId = "user-session-123";
const response1 = await mcp.executeScript({
    sessionId,
    script: `
        set userName [mcp::context get user.name]
        puts "Hello $userName! What project are you working on?"
        mcp::session set "conversation_state" "awaiting_project"
    `
});

const response2 = await mcp.executeScript({
    sessionId,  
    script: `
        set projectName "MyProject"
        mcp::session set "current_project" $projectName
        set bugs [mcp::call_tool "get_active_bugs" [dict create projectId $projectName]]
        puts "Found [llength $bugs] active bugs in $projectName"
    `
});
```

**Success Criteria**:
- ✅ The server accepts `scripts/execute` requests
- ✅ A variable set in one `scripts/execute` call with a sessionId is accessible in a subsequent call with the same sessionId
- ✅ A script with `streaming: true` can send multiple puts commands that are received incrementally by the client

### 3.2 Event-Driven Background Scripts (Week 11-12)

**Goal**: Allow Eagle scripts to run as persistent background services that react to server-side events.

**Implementation**: Create an `EagleEventBridge` that subscribes to MediatR INotifications and triggers corresponding Eagle event handlers.

```csharp
// Create new file: /src/DevOpsMcp.Application/Eagle/EagleEventBridge.cs
public class EagleEventBridge : 
    INotificationHandler<BuildCompletedEvent>,
    INotificationHandler<DeploymentFailedEvent>
{
    private readonly EagleEventSystem _eventSystem;
    private readonly ILogger<EagleEventBridge> _logger;

    public EagleEventBridge(EagleEventSystem eventSystem, ILogger<EagleEventBridge> logger)
    {
        _eventSystem = eventSystem;
        _logger = logger;
    }

    public async Task Handle(BuildCompletedEvent notification, CancellationToken cancellationToken)
    {
        var eventData = new Dictionary<string, object>
        {
            ["projectId"] = notification.ProjectId,
            ["buildId"] = notification.BuildId,
            ["status"] = notification.Status.ToString(),
            ["timestamp"] = notification.Timestamp
        };
        
        await _eventSystem.TriggerEvent("build_completed", eventData);
    }
    
    public async Task Handle(DeploymentFailedEvent notification, CancellationToken cancellationToken)
    {
        var eventData = new Dictionary<string, object>
        {
            ["projectId"] = notification.ProjectId,
            ["deploymentId"] = notification.DeploymentId,
            ["environment"] = notification.Environment,
            ["error"] = notification.ErrorMessage
        };
        
        await _eventSystem.TriggerEvent("deployment_failed", eventData);
    }
}
```

```csharp
// Create new file: /src/DevOpsMcp.Infrastructure/Eagle/EagleEventSystem.cs
public class EagleEventSystem
{
    private readonly ConcurrentDictionary<string, List<EagleEventHandler>> _handlers = new();
    private readonly ConcurrentDictionary<string, EagleSession> _servicesSessions = new();
    private readonly ILogger<EagleEventSystem> _logger;

    public EagleEventSystem(ILogger<EagleEventSystem> logger)
    {
        _logger = logger;
    }

    public void Subscribe(string eventName, string sessionId, string callbackProcedure)
    {
        var handler = new EagleEventHandler(sessionId, callbackProcedure, this);
        
        _handlers.AddOrUpdate(eventName, 
            new List<EagleEventHandler> { handler },
            (key, existing) => { existing.Add(handler); return existing; });
            
        _logger.LogInformation("Subscribed to event {EventName} with callback {Callback}", eventName, callbackProcedure);
    }

    public async Task TriggerEvent(string eventName, object eventData)
    {
        if (_handlers.TryGetValue(eventName, out var handlers))
        {
            var tasks = handlers.Select(handler => handler.ExecuteAsync(eventData));
            await Task.WhenAll(tasks);
        }
    }
    
    public void RegisterService(string serviceName, EagleSession session)
    {
        _servicesSessions[serviceName] = session;
        _logger.LogInformation("Registered Eagle service: {ServiceName}", serviceName);
    }
}

public class EagleEventHandler
{
    private readonly string _sessionId;
    private readonly string _callbackProcedure;
    private readonly EagleEventSystem _eventSystem;
    
    public EagleEventHandler(string sessionId, string callbackProcedure, EagleEventSystem eventSystem)
    {
        _sessionId = sessionId;
        _callbackProcedure = callbackProcedure;
        _eventSystem = eventSystem;
    }
    
    public async Task ExecuteAsync(object eventData)
    {
        // Find the session and execute the callback procedure
        if (_eventSystem._servicesSessions.TryGetValue(_sessionId, out var session))
        {
            var script = $"{_callbackProcedure} {{{ConvertToTclDict(eventData)}}}";
            await session.ExecuteAsync(script, CancellationToken.None);
        }
    }
    
    private string ConvertToTclDict(object data)
    {
        // Convert C# object to Tcl dictionary format
        var json = JsonSerializer.Serialize(data);
        return ConvertJsonToTclDict(json);
    }
}
```

**Eagle Commands for Event Handling**:
```csharp
// Add to existing Eagle command set
public class EventsCommand : IEagleCommand
{
    private readonly EagleEventSystem _eventSystem;
    
    public EventsCommand(EagleEventSystem eventSystem) => _eventSystem = eventSystem;
    
    public ReturnCode Execute(Interpreter interpreter, IClientData clientData, ArgumentList arguments, ref Result result)
    {
        if (arguments.Count < 3)
        {
            result = "Usage: mcp::events subscribe <event_name> <callback_procedure>";
            return ReturnCode.Error;
        }
        
        var action = arguments[1].ToString();
        
        if (action == "subscribe")
        {
            var eventName = arguments[2].ToString();
            var callback = arguments[3].ToString();
            var sessionId = GetSessionId(interpreter); // Get session from interpreter context
            
            _eventSystem.Subscribe(eventName, sessionId, callback);
            result = $"Subscribed to {eventName}";
            return ReturnCode.Ok;
        }
        
        result = "Unknown events action";
        return ReturnCode.Error;
    }
}
```

**Eagle Service Script** (`/eagle-services/build_monitor.eagle`):
```tcl
# MCP-Service-Name: build_monitor
# MCP-Service-Description: "Monitors build events and responds automatically"

# Subscribe to a server-side event and link it to a Tcl procedure
mcp::events subscribe "build_completed" [list handle_build_completed]

proc handle_build_completed {event_data} {
    set status [dict get $event_data status]
    if {$status eq "Failed"} {
        set projectId [dict get $event_data projectId]
        set buildId [dict get $event_data buildId]
        
        # Auto-create work item for build failure
        set workItem [mcp::call_tool "create_work_item" [dict create \
            projectId $projectId \
            workItemType "Bug" \
            title "Automated Bug: Build $buildId failed" \
            description "Build failed automatically detected by build monitor" \
        ]]
        
        # Log the action
        puts "Created bug work item $workItem for failed build $buildId"
    }
}

# Register this script as a long-running service
mcp::service register "build_monitor"
puts "Build monitor service started and listening for events"
```

**Success Criteria**:
- ✅ Placing an .eagle file in the `/eagle-services` directory starts a background script
- ✅ When a build fails in Azure DevOps, the BuildCompletedEvent is published, triggering the handle_build_completed procedure in the script
- ✅ A new bug work item is created automatically in response to the event

## Phase 4: Ecosystem and Production Features (Weeks 13-16)

### 4.1 Script Marketplace (Week 13)

**Goal**: Implement a system for discovering, sharing, and installing community and official Eagle scripts.

**Implementation**: Create a new MCP tool that interacts with Git repositories defined in a config file.

```yaml
# Create: /config/marketplace.yaml
repositories:
  - name: official
    url: https://github.com/your-org/eagle-mcp-scripts
    trust_level: verified
    auto_update: true
    
  - name: community
    url: https://github.com/community/eagle-scripts  
    trust_level: community
    auto_update: false

categories:
  - devops
  - monitoring
  - deployment
  - testing
  - automation
```

```tcl
# Create: /eagle-tools/discover_scripts.eagle
# MCP-Tool-Name: discover_scripts
# MCP-Tool-Description: "Browse and install Eagle scripts from marketplace"
# MCP-Tool-InputSchema: {
#   "type": "object", 
#   "properties": {
#     "category": {"type": "string"},
#     "search": {"type": "string"},
#     "repository": {"type": "string"}
#   }
# }

# Load marketplace configuration
set config_file "config/marketplace.yaml"
if {![file exists $config_file]} {
    error "Marketplace configuration not found"
}

# Parse search parameters
set category [dict get $args category ""]
set search [dict get $args search ""]
set repository [dict get $args repository ""]

# Use .NET library to interact with Git repos
set gitLib [object create LibGit2Sharp.Repository]

# Scan repositories for scripts
set scripts [list]
foreach repo [get_marketplace_repos] {
    set repo_scripts [scan_repository_scripts $repo $category $search]
    set scripts [concat $scripts $repo_scripts]
}

set result [dict create \
    scripts $scripts \
    categories [list "devops" "monitoring" "deployment" "testing" "automation"] \
    repositories [get_marketplace_repos] \
]

return [to_json $result]
```

```tcl
# Create: /eagle-tools/install_script.eagle  
# MCP-Tool-Name: install_script
# MCP-Tool-Description: "Install an Eagle script from the marketplace"
# MCP-Tool-InputSchema: {
#   "type": "object",
#   "properties": {
#     "scriptId": {"type": "string"},
#     "repository": {"type": "string"},
#     "destination": {"type": "string", "enum": ["tools", "services"]}
#   },
#   "required": ["scriptId", "repository"]
# }

set scriptId [dict get $args scriptId]
set repository [dict get $args repository]
set destination [dict get $args destination "tools"]

# Validate repository trust level
set repo_info [get_repository_info $repository]
if {[dict get $repo_info trust_level] eq "untrusted"} {
    error "Cannot install from untrusted repository"
}

# Download and install script
set script_content [download_script $repository $scriptId]
set dest_dir [expr {$destination eq "tools" ? "./eagle-tools" : "./eagle-services"}]
set dest_file "$dest_dir/$scriptId.eagle"

# Write script file (this will trigger hot reload)
set fp [open $dest_file w]
puts $fp $script_content
close $fp

return [dict create \
    status "installed" \
    scriptId $scriptId \
    destination $dest_file \
    message "Script installed and ready to use" \
]
```

### 4.2 Debugging and Testing Tools (Week 14-15)

**Goal**: Provide professional development tools to ensure high-quality, reliable Eagle scripts.

```tcl
# Create: /eagle-tools/debug_eagle_script.eagle
# MCP-Tool-Name: debug_eagle_script
# MCP-Tool-Description: "Interactive debugger for Eagle scripts"
# MCP-Tool-InputSchema: {
#   "type": "object",
#   "properties": {
#     "script": {"type": "string"},
#     "breakpoints": {"type": "array", "items": {"type": "object"}},
#     "sessionId": {"type": "string"}
#   },
#   "required": ["script"]
# }

set script [dict get $args script]
set breakpoints [dict get $args breakpoints [list]]
set sessionId [dict get $args sessionId "debug-[clock seconds]"]

# Initialize Eagle debugger
debug enable

# Set breakpoints  
foreach bp $breakpoints {
    set line [dict get $bp line]
    set file [dict get $bp file "memory"]
    debug breakpoint set -line $line -file $file
}

# Execute with debugging in isolated session
try {
    set result [eval $script]
    
    return [dict create \
        status "completed" \
        result $result \
        sessionId $sessionId \
    ]
} trap {DEBUG BREAKPOINT} {msg} {
    return [dict create \
        status "breakpoint" \
        location $msg \
        variables [debug variables] \
        stack [debug stack] \
        sessionId $sessionId \
    ]
} on error {msg} {
    return [dict create \
        status "error" \
        error $msg \
        stack [debug stack] \
        sessionId $sessionId \
    ]
}
```

```tcl
# Create: /eagle-tools/test_eagle_scripts.eagle
# MCP-Tool-Name: test_eagle_scripts
# MCP-Tool-Description: "Run tests for Eagle MCP tools"
# MCP-Tool-InputSchema: {
#   "type": "object",
#   "properties": {
#     "testSuite": {"type": "string"},
#     "testPattern": {"type": "string"}
#   }
# }

# Eagle-based testing framework
namespace eval ::testing {
    variable test_results [list]
    variable current_test ""
    
    proc test {name script expected} {
        variable test_results
        variable current_test
        
        set current_test $name
        set start_time [clock milliseconds]
        
        try {
            set result [uplevel 1 $script]
            set end_time [clock milliseconds]
            set duration [expr {$end_time - $start_time}]
            
            if {$result eq $expected} {
                lappend test_results [dict create \
                    name $name \
                    status "passed" \
                    duration $duration \
                ]
                puts "PASS: $name"
            } else {
                lappend test_results [dict create \
                    name $name \
                    status "failed" \
                    expected $expected \
                    actual $result \
                    duration $duration \
                ]
                puts "FAIL: $name - Expected '$expected', got '$result'"
            }
        } on error {msg} {
            set end_time [clock milliseconds]
            set duration [expr {$end_time - $start_time}]
            
            lappend test_results [dict create \
                name $name \
                status "error" \
                error $msg \
                duration $duration \
            ]
            puts "ERROR: $name - $msg"
        }
    }
    
    proc assert_equal {expected actual {message ""}} {
        if {$expected ne $actual} {
            error "Assertion failed: Expected '$expected', got '$actual'. $message"
        }
    }
    
    proc mock_tool {tool_name response} {
        # Override mcp::call_tool for testing
        proc ::mcp::call_tool {name args} [list return $response]
    }
    
    proc run_tests {} {
        variable test_results
        
        set total [llength $test_results]
        set passed [llength [lsearch -all -inline $test_results *status*passed*]]
        set failed [expr {$total - $passed}]
        
        return [dict create \
            total $total \
            passed $passed \
            failed $failed \
            results $test_results \
        ]
    }
}

# Load and run test suite
set testSuite [dict get $args testSuite "all"]
set testPattern [dict get $args testPattern "*"]

# Example test suite
if {$testSuite eq "all" || $testSuite eq "core"} {
    ::testing::test "context_access" {
        mcp::context get user.name
    } "TestUser"
    
    ::testing::test "session_persistence" {
        mcp::session set "test_key" "test_value"
        mcp::session get "test_key"
    } "test_value"
}

return [::testing::run_tests]
```

### 4.3 Documentation and Community (Week 16)

**Goal**: Finalize the ecosystem with auto-generated documentation and clear contribution pathways.

```tcl
# Create: /eagle-tools/generate_docs.eagle
# MCP-Tool-Name: generate_docs  
# MCP-Tool-Description: "Generate documentation from Eagle script metadata"
# MCP-Tool-InputSchema: {
#   "type": "object",
#   "properties": {
#     "directory": {"type": "string", "default": "./eagle-tools"},
#     "format": {"type": "string", "enum": ["markdown", "html", "json"], "default": "markdown"}
#   }
# }

set directory [dict get $args directory "./eagle-tools"]
set format [dict get $args format "markdown"]

proc extract_metadata {file_content} {
    set metadata [dict create]
    set lines [split $file_content \n]
    
    foreach line $lines {
        set trimmed [string trim $line]
        if {[string match "# MCP-Tool-*" $trimmed]} {
            set parts [split $trimmed ":"]
            if {[llength $parts] >= 2} {
                set key [string trim [string range [lindex $parts 0] 2 end]]
                set value [string trim [join [lrange $parts 1 end] ":"]]
                dict set metadata $key $value
            }
        }
    }
    
    return $metadata
}

proc extract_examples {file_content} {
    # Extract example usage from comments
    set examples [list]
    set lines [split $file_content \n]
    set in_example false
    set current_example ""
    
    foreach line $lines {
        if {[string match "*# Example:*" $line]} {
            set in_example true
            set current_example ""
        } elseif {$in_example && [string match "#*" $line]} {
            append current_example [string range $line 1 end] \n
        } elseif {$in_example && ![string match "#*" $line]} {
            if {$current_example ne ""} {
                lappend examples [string trim $current_example]
            }
            set in_example false
        }
    }
    
    return $examples
}

# Generate documentation for all Eagle tools
set docs [list]

foreach script_file [glob -nocomplain $directory/*.eagle] {
    set file_content [read [set fp [open $script_file r]]; close $fp]
    set metadata [extract_metadata $file_content]
    
    if {[dict exists $metadata "MCP-Tool-Name"]} {
        set doc [dict create \
            name [dict get $metadata "MCP-Tool-Name"] \
            description [dict get $metadata "MCP-Tool-Description" ""] \
            inputSchema [dict get $metadata "MCP-Tool-InputSchema" "{}"] \
            examples [extract_examples $file_content] \
            file [file tail $script_file] \
        ]
        lappend docs $doc
    }
}

# Format output based on requested format
switch $format {
    "markdown" {
        set output "# Eagle MCP Tools Documentation\n\n"
        append output "Generated on [clock format [clock seconds]]\n\n"
        
        foreach doc $docs {
            append output "## [dict get $doc name]\n\n"
            append output "[dict get $doc description]\n\n"
            append output "**File:** `[dict get $doc file]`\n\n"
            
            set schema [dict get $doc inputSchema]
            if {$schema ne "{}"} {
                append output "**Input Schema:**\n```json\n$schema\n```\n\n"
            }
            
            set examples [dict get $doc examples]
            if {[llength $examples] > 0} {
                append output "**Examples:**\n"
                foreach example $examples {
                    append output "```tcl\n$example\n```\n\n"
                }
            }
            
            append output "---\n\n"
        }
        
        return $output
    }
    "json" {
        return [dict create \
            tools $docs \
            generated_at [clock format [clock seconds]] \
            total_tools [llength $docs] \
        ]
    }
    default {
        error "Unsupported format: $format"
    }
}
```

**Create Contribution Guide**:
```markdown
# Create: CONTRIBUTING_EAGLE.md

# Contributing Eagle Scripts to the MCP Server

## Overview

Eagle scripts are the heart of our MCP server's extensibility. This guide explains how to write, test, and contribute high-quality Eagle scripts.

## Script Structure

Every Eagle MCP tool must follow this structure:

```tcl
# MCP-Tool-Name: your_tool_name
# MCP-Tool-Description: "Brief description of what the tool does"
# MCP-Tool-InputSchema: { "type": "object", "properties": {...}, "required": [...] }

# 1. Input validation
if {![dict exists $args requiredParam]} {
    error "Missing required parameter: requiredParam"
}

# 2. Context access
set userId [mcp::context get user.id]
set projectId [mcp::context get project.id]

# 3. Core logic
set result [mcp::call_tool "other_tool" $params]

# 4. Structured output  
return [to_json [dict create \
    status "success" \
    data $result \
    metadata [dict create timestamp [clock seconds]] \
]]
```

## Testing Your Scripts

Use the built-in testing framework:

```tcl
# test_your_tool.tcl
source testing_framework.tcl

::testing::test "basic_functionality" {
    # Mock dependencies
    ::testing::mock_tool "other_tool" {"mocked_result"}
    
    # Test your tool
    source your_tool.eagle
    # ... test logic ...
} "expected_result"

puts [::testing::run_tests]
```

## Submission Process

1. Write your Eagle script following the standards
2. Test thoroughly using the testing framework
3. Submit PR to the community repository
4. Include documentation and examples
5. Respond to review feedback

## Best Practices

- Always validate inputs
- Use structured error messages
- Include comprehensive documentation
- Follow naming conventions
- Test edge cases thoroughly
```

**Success Criteria**:
- ✅ Marketplace installation works reliably
- ✅ Debugging tools provide comprehensive script analysis
- ✅ Testing framework supports unit and integration tests
- ✅ Documentation generation produces complete API docs
- ✅ Contribution process is clear and well-documented

## Implementation Guidelines for AI Assistant

### Required Context for Implementation

When implementing this blueprint, I will need access to:

**1. Existing Codebase Context**:
- Current file structure (`/src/DevOpsMcp.Infrastructure/`, `/src/DevOpsMcp.Server/`, etc.)
- Existing classes (`EagleScriptExecutor`, `EagleExecutionTool`, `IMessageHandler`, `IPersonaMemoryManager`)
- Current Eagle integration patterns and dependency injection setup
- Existing DevOpsContext and domain model structure

**2. Integration Points**:
- How `IToolRegistry` currently works for tool registration
- MediatR command/query patterns in use
- Current authentication and authorization patterns
- Existing logging and error handling strategies

**3. Development Standards**:
- Project coding conventions and naming patterns
- Existing test frameworks and patterns
- CI/CD pipeline requirements
- Documentation standards

### Implementation Priorities

**Phase-by-Phase Focus**:
1. **Phase 1**: Enhance existing `EagleScriptExecutor` before adding new features
2. **Phase 2**: Build on existing tool infrastructure (`IToolRegistry`, `ITool`)
3. **Phase 3**: Extend existing `MessageHandler` rather than replacing it
4. **Phase 4**: Follow existing patterns for hosted services and configuration

**Code Quality Standards**:
- Use dependency injection consistently with existing patterns
- Follow existing error handling and logging approaches
- Maintain backward compatibility with current Eagle script execution
- Ensure proper resource disposal and memory management
- Add comprehensive unit and integration tests

**Security Considerations**:
- Maintain Eagle sandbox integrity throughout all phases
- Validate all user inputs and script metadata
- Implement proper session isolation
- Add audit logging for script execution and tool registration
- Follow principle of least privilege for script capabilities

### Success Metrics by Phase

**Phase 1 Success Criteria**:
- ✅ `mcp::context get user.name` returns actual user name from DevOpsContext
- ✅ Session state persists across multiple `scripts/execute` calls
- ✅ `mcp::call_tool "list_projects" [dict create]` successfully calls existing tool
- ✅ Script returning JSON dictionary creates structured CallToolResponse

**Phase 2 Success Criteria**:
- ✅ File dropped in `/eagle-tools/` appears in `tools/list` within 5 seconds
- ✅ Metadata changes trigger tool re-registration automatically
- ✅ All 12 core Eagle tools are functional and demonstrate best practices
- ✅ Dynamic tools can call other tools and access full context

**Phase 3 Success Criteria**:
- ✅ `scripts/execute` method works alongside existing MCP methods
- ✅ Session variables persist across script calls with same sessionId
- ✅ Event-driven scripts respond to domain events (BuildCompletedEvent, etc.)
- ✅ Background services can be registered and managed properly

**Phase 4 Success Criteria**:
- ✅ Script installation from marketplace works end-to-end
- ✅ Debugging tools provide breakpoint and variable inspection
- ✅ Testing framework can mock tools and validate script behavior
- ✅ Documentation generation produces complete, accurate API docs

### Anti-Patterns to Avoid

**Architecture Anti-Patterns**:
- ❌ Creating parallel systems outside Eagle integration
- ❌ Bypassing existing dependency injection patterns
- ❌ Breaking existing tool interfaces or contracts
- ❌ Compromising Eagle sandbox security for convenience

**Implementation Anti-Patterns**:
- ❌ Hard-coding file paths or configuration values
- ❌ Ignoring existing error handling patterns
- ❌ Creating features that could be implemented as Eagle scripts
- ❌ Breaking backward compatibility without migration path

**Performance Anti-Patterns**:
- ❌ Creating new interpreter instances for every script execution
- ❌ Blocking the main thread with long-running script operations
- ❌ Loading large assemblies repeatedly instead of pre-loading
- ❌ Missing resource disposal or cleanup in session management

## Conclusion

This comprehensive blueprint transforms your MCP server from basic Eagle script execution into a revolutionary DevOps automation platform. The integration guidance provides concrete implementation details that align with your existing codebase structure and patterns.

**Key Transformation Achieved**:

1. **Eagle as Foundation**: Every DevOps capability flows through Eagle scripting, making it the primary automation interface
2. **Dynamic Extensibility**: Scripts become tools, tools become conversations, conversations become autonomous agents
3. **Production-Ready**: Complete ecosystem with marketplace, debugging, testing, and documentation
4. **Enterprise-Grade**: Maintains security, performance, and reliability standards throughout

**Unique Competitive Advantages**:

- **Zero-Downtime Extension**: Add new automation capabilities without server restarts
- **Conversational DevOps**: Natural language interfaces for complex operations
- **Self-Healing Infrastructure**: Event-driven autonomous response systems
- **Unified Scripting Platform**: Single language across all DevOps automation
- **Community-Driven Innovation**: Marketplace ecosystem for shared solutions

The implementation follows your existing architectural patterns while introducing revolutionary capabilities. Each phase builds incrementally, ensuring stability and allowing for course corrections based on real-world usage.

This Eagle-powered MCP server represents a paradigm shift toward **intelligent, conversational, and infinitely extensible** DevOps automation that adapts and grows with organizational needs.