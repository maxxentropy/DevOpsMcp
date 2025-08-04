# Eagle Test Suite for DevOps MCP Server

This directory contains the comprehensive test suite for the Eagle integration in the DevOps MCP Server.

## Test Structure

```
tests/Eagle/
├── TestScripts/              # Eagle/Tcl test scripts
│   ├── Phase1Complete.test.tcl        # Main Phase 1 functionality test
│   ├── RichContext.test.tcl           # Tests mcp::context and mcp::session
│   ├── StructuredOutput.test.tcl      # Tests mcp::output formatting
│   ├── DeepContextPaths.test.tcl      # Tests deep context access (e.g., project.lastBuild.status)
│   ├── SecurityPolicy.test.tcl        # Tests security restrictions
│   ├── SessionPersistence.test.tcl    # Sets values for persistence testing
│   ├── SessionPersistenceVerify.test.tcl # Verifies persistence after restart
│   └── InterpreterPool.test.tcl       # Tests basic pool operations
└── TestRunners/              # Python test execution utilities
    ├── run_eagle_test.py              # Generic test runner
    ├── run_security_test.py           # Security-specific test runner
    ├── run_all_security_tests.sh      # Runs all security levels
    └── test_pool_concurrent.py        # Concurrent pool stress test
```

## Running Tests

### Prerequisites

1. Build and start the Docker container:
```bash
docker build -t devops-mcp .
docker run -p 8080:8080 devops-mcp
```

### Using the Generic Test Runner

The `run_eagle_test.py` script can run any Eagle test:

```bash
# Run a basic test
python3 TestRunners/run_eagle_test.py Phase1Complete.test.tcl

# Run with specific security level
python3 TestRunners/run_eagle_test.py SecurityPolicy.test.tcl --security Minimal

# Run with session ID (for persistence testing)
python3 TestRunners/run_eagle_test.py SessionPersistenceVerify.test.tcl --session-id abc123

# Run with different output format
python3 TestRunners/run_eagle_test.py StructuredOutput.test.tcl --format json
```

### Security Testing

Test all security levels:
```bash
./TestRunners/run_all_security_tests.sh
```

Or test individual levels:
```bash
python3 TestRunners/run_security_test.py Minimal
python3 TestRunners/run_security_test.py Standard
python3 TestRunners/run_security_test.py Elevated
python3 TestRunners/run_security_test.py Maximum
```

### Session Persistence Testing

1. Set session values:
```bash
python3 TestRunners/run_eagle_test.py SessionPersistence.test.tcl
# Note the session ID from the output
```

2. Restart the Docker container

3. Verify persistence:
```bash
python3 TestRunners/run_eagle_test.py SessionPersistenceVerify.test.tcl --session-id <noted-session-id>
```

### Interpreter Pool Testing

Basic pool test:
```bash
python3 TestRunners/run_eagle_test.py InterpreterPool.test.tcl
```

Concurrent stress test:
```bash
python3 TestRunners/test_pool_concurrent.py
```

## Test Descriptions

### Phase 1 Core Tests

1. **Phase1Complete.test.tcl**
   - Verifies core Phase 1 functionality
   - Tests environment variables, working directory, package readiness
   - Tests MCP tool calling and context access
   - Tests output formatting

2. **RichContext.test.tcl**
   - Comprehensive test of mcp::context command
   - Tests session state management (set, get, list, clear)
   - Tests tool calling interface
   - Tests error handling

3. **StructuredOutput.test.tcl**
   - Tests all output formats: JSON, XML, YAML, Table, CSV
   - Verifies formatting of various data structures
   - Tests multiple formats with same data

### Extended Tests

4. **DeepContextPaths.test.tcl**
   - Tests deep context access like "project.lastBuild.status"
   - Verifies nested path resolution
   - Tests non-existent path handling

5. **SecurityPolicy.test.tcl**
   - Tests security restrictions at different levels
   - Verifies file access, command execution, networking restrictions
   - Tests environment variable access and interpreter creation

6. **SessionPersistence.test.tcl** / **SessionPersistenceVerify.test.tcl**
   - Tests SQLite-based session persistence
   - Verifies data survives container restarts
   - Tests various data types (strings, numbers, lists, complex structures)

7. **InterpreterPool.test.tcl**
   - Tests basic interpreter pool functionality
   - Verifies pool can handle various operations
   - Tests session operations for thread safety

## Expected Results

All tests should pass with the following expectations:

- **Security Tests**: Should show appropriate restrictions at each level
  - Minimal: Most restrictive, no file/network/exec access
  - Standard: Balanced security (default)
  - Elevated: Less restrictive
  - Maximum: Least restrictive (use with caution)

- **Concurrent Tests**: All requests should complete successfully
  - Pool should handle 30 concurrent requests (3 waves of 10)
  - Response times should be reasonable (<1s typically)

- **Persistence Tests**: Session values should survive container restarts

## Troubleshooting

1. **Connection Refused**: Ensure Docker container is running on port 8080
2. **Test Not Found**: Check file paths, ensure you're in the correct directory
3. **Permission Denied**: Some tests may be restricted by security level
4. **Session Not Found**: Ensure you're using the correct session ID