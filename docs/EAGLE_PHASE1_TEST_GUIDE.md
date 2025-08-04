# Eagle Integration Phase 1 Test Guide

## Overview

This guide documents the testing functionality for Phase 1 of the Eagle integration, including test structure, coverage areas, and guidance for extending tests.

## Test Suite Structure

### Test Files Location
All Eagle test scripts are located in `/tests/Eagle/TestScripts/`:

1. **RichContext.test.tcl** - Tests context injection commands
2. **StructuredOutput.test.tcl** - Tests output formatting functionality
3. **Security.test.tcl** - Tests security policy enforcement
4. **SessionPersistence.test.tcl** - Tests session state persistence
5. **Phase1Complete.test.tcl** - Integration test for all Phase 1 features

### Test Framework

Each test script includes a minimal test framework with these procedures:

```tcl
proc assert {condition message} {
    global testsPassed testsFailed
    if {$condition} {
        puts "✓ $message"
        incr testsPassed
    } else {
        puts "✗ $message"
        incr testsFailed
    }
}

proc assertEq {actual expected message} {
    assert [expr {$actual eq $expected}] "$message (expected: '$expected', got: '$actual')"
}

proc summary {} {
    global testsPassed testsFailed
    set total [expr {$testsPassed + $testsFailed}]
    puts "\n=========================================="
    puts "Test Summary: $testsPassed/$total passed"
    if {$testsFailed > 0} {
        puts "FAILED: $testsFailed tests failed"
        return 1
    } else {
        puts "SUCCESS: All tests passed"
        return 0
    }
}
```

## Phase 1 Feature Coverage

### 1. Rich Context Injection (100% Coverage)
- **Commands Tested**: `mcp::context get`, `mcp::session`, `mcp::call_tool`
- **Test Coverage**:
  - Command existence verification
  - Context data retrieval (user, project, environment)
  - Boolean value handling
  - Session state management (set, get, list, clear)
  - Error handling for invalid operations

### 2. Structured Output Processing (100% Coverage)
- **Commands Tested**: `mcp::output`
- **Test Coverage**:
  - All 6 output formats (JSON, XML, YAML, Table, CSV, Markdown)
  - Simple and complex data structures
  - Array and nested object handling
  - Format-specific validation

### 3. Security Sandboxing (100% Coverage)
- **Security Levels Tested**: Minimal, Standard, Elevated, Maximum
- **Test Coverage**:
  - Command availability at each security level
  - File system access restrictions
  - Network operation restrictions
  - Process execution restrictions
  - CLR reflection restrictions

### 4. Session Persistence (100% Coverage)
- **Functionality Tested**: SQLite-based session storage
- **Test Coverage**:
  - Session value persistence across executions
  - Complex data type persistence
  - Session ID propagation
  - Verification after container restart

## Running Tests

### Individual Test Execution
```bash
cd /tests/Eagle
./run_test.sh "Test Name" "TestFile.test.tcl" "--security Level"
```

### Full Test Suite
```bash
cd /tests/Eagle
./run_all_tests.sh
```

### Docker Container Testing
```bash
docker-compose up -d
docker exec eagle-mcp /tests/Eagle/run_all_tests.sh
```

## Missing Test Functionality

### 1. Negative Test Cases
Currently missing comprehensive negative testing for:
- Invalid security levels
- Malformed structured output data
- Session corruption scenarios
- Concurrent session access

### 2. Performance Testing
No current tests for:
- Interpreter pool exhaustion
- Large data set handling in structured output
- Session store performance under load
- Security policy enforcement overhead

### 3. Edge Cases
Limited coverage for:
- Unicode and special character handling
- Very large session values
- Deeply nested data structures
- Binary data in sessions

### 4. Integration Testing
Could benefit from:
- Multi-session interaction tests
- Security level transition tests
- Output format chaining tests
- Real Azure DevOps API integration tests

## Extending the Test Suite

### Adding New Tests

1. **Create Test File**:
   ```tcl
   # tests/Eagle/TestScripts/YourFeature.test.tcl
   source [file join [file dirname $argv0] test_framework.tcl]
   
   puts "Testing Your Feature"
   puts "===================="
   
   # Your test cases here
   assert [expr {1 == 1}] "Basic assertion works"
   
   summary
   ```

2. **Add to Test Runner**:
   ```bash
   # In run_all_tests.sh, add:
   run_test "Your Feature Test" "YourFeature.test.tcl"
   ```

### Testing Security Levels

When testing features that require elevated permissions:
```bash
run_test "Elevated Feature" "Feature.test.tcl" "--security Elevated"
```

### Testing Session Persistence

1. Run the setup script to create session data
2. Note the session ID from output
3. Restart the container
4. Run verification script with same session ID

### Testing Structured Output

Use the `mcp::output` command with different formats:
```tcl
set data {{"key": "value"}}
puts [mcp::output $data json]
puts [mcp::output $data xml]
puts [mcp::output $data yaml]
```

## Best Practices

1. **Always use the test framework** - Don't rely on manual output inspection
2. **Test both success and failure paths** - Verify error handling
3. **Use descriptive test names** - Make failures easy to diagnose
4. **Clean up test state** - Use `mcp::session clear` when needed
5. **Document expected behavior** - Add comments explaining test intent

## Debugging Failed Tests

1. **Check security level** - Ensure the test has appropriate permissions
2. **Verify command availability** - Use `info commands mcp::*` to list available commands
3. **Check session state** - Use `mcp::session list` to inspect current session
4. **Review execution logs** - Container logs show detailed execution information

## Future Test Improvements

### Phase 2 Testing Needs
- Dynamic tool registration tests
- Parameter validation tests
- Error propagation tests
- Tool discovery tests

### Phase 3 Testing Needs
- Performance optimization verification
- Advanced feature toggle tests
- Resource usage monitoring
- Concurrency stress tests

### Phase 4 Testing Needs
- Workflow automation tests
- Advanced security policy tests
- Multi-interpreter coordination tests
- DevOps-specific scenario tests