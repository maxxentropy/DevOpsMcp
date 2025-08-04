# Phase 1 Missing/Untested Features

## Actually Implemented but Not Tested:

### 1. Package Import
- ✅ Implementation exists in EagleScriptExecutor.ImportPackagesAsync
- ✅ Uses `package require` command
- ❌ Not tested in Phase1Complete.test.tcl (comment says "will be tested when packages available")
- **Action**: Test with actual Eagle packages

### 2. Environment Variables 
- ✅ Implementation exists in EagleScriptExecutor.InjectEnvironmentVariablesAsync
- ✅ Tested in Phase1Complete.test.tcl with TEST_VAR
- ✅ Working correctly

### 3. Working Directory
- ✅ Implementation exists in EagleScriptExecutor.SetWorkingDirectoryAsync
- ✅ Tested in Phase1Complete.test.tcl 
- ✅ Working correctly (set to /tmp)

### 4. Execution History
- ✅ ExecutionHistoryStore implemented and storing history
- ✅ eagle_history tool registered in MCP
- ❌ Not tested
- **Action**: Test the eagle_history tool

## Features Needing Testing/Verification:

### 1. Session Persistence Across Restarts
- Implementation exists (SQLite-based)
- Need to verify sessions survive container restarts
- **Test**: Set session value, restart container, retrieve value

### 2. Security Policy Enforcement
- SecurityLevel parameter exists (Minimal, Standard, Restricted, Paranoid)
- Need to verify each level properly restricts operations
- **Test**: Try dangerous operations at each security level

### 3. Interpreter Pool Management
- Pool exists with configurable limits
- Need to test concurrent execution, pool exhaustion, cleanup
- **Test**: Run many concurrent scripts, verify pool behavior

### 4. Structured Output Auto-Detection (Phase 1.2)
- Currently requires explicit mcp::output command
- Blueprint wants automatic detection of returned Tcl dicts
- **Missing**: Auto-conversion of script return values to JSON

## Partially Implemented Features:

### 1. Output Formatting
- ✅ mcp::output command works for all formats
- ❌ XML/CSV formatters don't parse JSON structure (just wrap content)
- **Limitation**: Formatters treat input as strings, not structured data

### 2. Context Access
- ✅ Basic context works (user, organization, project, environment)
- ❌ Deep paths like "project.lastBuild.status" not implemented
- **Limitation**: Only predefined context values available

## Summary:
Most Phase 1 features are actually implemented! The main gaps are:
1. Testing of already-implemented features (packages, history, security)
2. Automatic structured output detection 
3. Deep context path access
4. Proper JSON parsing in XML/CSV formatters

The tests pass because they were adapted to work with current limitations.