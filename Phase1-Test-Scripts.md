# Phase 1 Test Scripts

This document lists all the test scripts created to verify Phase 1 functionality.

## Official Phase 1 Tests (Already Passing)
1. **Phase1Complete.test.tcl** - Main Phase 1 functionality test
2. **RichContext.test.tcl** - Tests mcp::context and mcp::session commands
3. **StructuredOutput.test.tcl** - Tests mcp::output command with various formats

## Additional Test Scripts Created

### 1. Deep Context Path Access
**File**: `/tmp/devops-mcp-tests/DeepContextPaths.test.tcl`
**Purpose**: Tests deep context path access like "project.lastBuild.status"
**Tests**:
- Basic context access (user.name, project.name)
- Deep paths: project.lastBuild.status, project.lastBuild.id, project.lastBuild.date
- Repository paths: project.repository.url, project.repository.branch
- Non-existent path handling

### 2. Security Policy Enforcement
**File**: `/tmp/devops-mcp-tests/SecurityPolicy.test.tcl`
**Purpose**: Tests security restrictions at different levels
**Tests**:
- Safe operations (math, lists)
- File operations (read/write)
- Command execution
- Network operations (sockets)
- Environment variable access
- Interpreter creation

**Test Runners**:
- `test-security-minimal.py` - Tests Minimal security level
- `test-security-standard.py` - Tests Standard security level
- `test-security-elevated.py` - Tests Elevated security level
- `test-security-maximum.py` - Tests Maximum security level
- `run-security-tests.sh` - Runs all security tests

### 3. Session Persistence
**Files**:
- `/tmp/devops-mcp-tests/SessionPersistence.test.tcl` - Sets session values
- `/tmp/devops-mcp-tests/SessionPersistenceVerify.test.tcl` - Verifies persistence after restart

**Purpose**: Tests SQLite session persistence across container restarts
**Process**:
1. Run SessionPersistence.test.tcl to set values
2. Note the session ID
3. Restart the container
4. Run SessionPersistenceVerify.test.tcl with same session ID

### 4. Interpreter Pool Management
**File**: `/tmp/devops-mcp-tests/InterpreterPool.test.tcl`
**Purpose**: Basic interpreter pool testing
**Tests**:
- Basic calculations
- Loop operations
- String manipulation
- List operations
- Session operations

**Concurrent Test**: `/tmp/devops-mcp-tests/test-pool-concurrent.py`
**Purpose**: Stress tests the interpreter pool with concurrent requests
**Tests**:
- Sends 10 concurrent requests in 3 waves
- Varies execution time with delays
- Measures response times
- Verifies pool handles concurrent load

## How to Run the Tests

### Prerequisites
1. Build and start the Docker container:
```bash
docker build -t devops-mcp .
docker run -p 8080:8080 devops-mcp
```

### Running Individual Tests
```bash
# Deep context paths
python3 /tmp/run-test.py /tmp/devops-mcp-tests/DeepContextPaths.test.tcl

# Security tests
bash /tmp/devops-mcp-tests/run-security-tests.sh

# Interpreter pool
python3 /tmp/run-test.py /tmp/devops-mcp-tests/InterpreterPool.test.tcl
python3 /tmp/devops-mcp-tests/test-pool-concurrent.py

# Session persistence (requires container restart between scripts)
python3 /tmp/run-test-with-session.py /tmp/devops-mcp-tests/SessionPersistence.test.tcl "new-session-id"
# Restart container
python3 /tmp/run-test-with-session.py /tmp/devops-mcp-tests/SessionPersistenceVerify.test.tcl "same-session-id"
```

### Expected Results
- All tests should complete without errors
- Security tests should show appropriate restrictions at each level
- Concurrent tests should handle all requests successfully
- Session values should persist across container restarts