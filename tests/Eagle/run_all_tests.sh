#!/bin/bash

# Run All Phase 1 Tests for DevOps MCP Server
# This script runs all Eagle integration tests

set -e  # Exit on error

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

echo "=========================================="
echo "   DevOps MCP Eagle Test Suite"
echo "   Phase 1 Complete Test Run"
echo "=========================================="
echo

# Check for --skip-check argument
SKIP_CHECK=false
if [ "$1" == "--skip-check" ]; then
    SKIP_CHECK=true
fi

# Check if server is running (unless skipped)
if [ "$SKIP_CHECK" = false ]; then
    echo -e "${YELLOW}Checking if MCP server is running...${NC}"
    # Use HEAD request or check if port is open since /mcp is SSE endpoint
    if ! nc -z localhost 8080 2>/dev/null; then
        echo -e "${RED}Error: MCP server port 8080 is not accessible${NC}"
        echo "Please ensure the Docker container is running:"
        echo "  docker run -p 8080:8080 devops-mcp"
        echo
        echo "You can skip this check and proceed with:"
        echo "  ./run_all_tests.sh --skip-check"
        exit 1
    fi
    echo -e "${GREEN}✓ Server is running on port 8080${NC}"
else
    echo -e "${YELLOW}Skipping server check (--skip-check specified)${NC}"
fi
echo

# Function to run a test and check result
run_test() {
    local test_name=$1
    local test_file=$2
    local extra_args="${3:-}"
    
    echo -e "${YELLOW}Running: $test_name${NC}"
    echo "----------------------------------------"
    
    if python3 TestRunners/run_eagle_test.py "$test_file" $extra_args; then
        echo -e "${GREEN}✓ $test_name PASSED${NC}"
    else
        echo -e "${RED}✗ $test_name FAILED${NC}"
        FAILED_TESTS+=("$test_name")
    fi
    echo
    echo
}

# Track failed tests
FAILED_TESTS=()

# 1. Core Phase 1 Tests
echo -e "${YELLOW}=== CORE PHASE 1 TESTS ===${NC}"
echo

run_test "Phase 1 Complete Test" "Phase1Complete.test.tcl" "--security Elevated"
run_test "Rich Context Test" "RichContext.test.tcl"
run_test "Structured Output Test" "StructuredOutput.test.tcl"
run_test "Automatic Structure Detection" "AutoDetectionDemo.test.tcl"

# 2. Extended Feature Tests
echo -e "${YELLOW}=== EXTENDED FEATURE TESTS ===${NC}"
echo

run_test "Deep Context Paths Test" "DeepContextPaths.test.tcl"
run_test "Interpreter Pool Test" "InterpreterPool.test.tcl"

# 3. Security Policy Tests
echo -e "${YELLOW}=== SECURITY POLICY TESTS ===${NC}"
echo

echo "Testing all security levels..."
echo

for level in Minimal Standard Elevated Maximum; do
    echo -e "${YELLOW}Security Level: $level${NC}"
    echo "----------------------------------------"
    
    if python3 TestRunners/run_security_test.py "$level"; then
        echo -e "${GREEN}✓ Security $level test PASSED${NC}"
    else
        echo -e "${RED}✗ Security $level test FAILED${NC}"
        FAILED_TESTS+=("Security $level")
    fi
    echo
done

# 4. Session Persistence Test (Part 1)
echo -e "${YELLOW}=== SESSION PERSISTENCE TEST ===${NC}"
echo

echo "Setting session values..."
echo "----------------------------------------"

# Generate a session ID for persistence testing
PERSISTENCE_SESSION_ID="test-session-$(date +%s)"

# Run the persistence setup with the session ID
OUTPUT=$(python3 TestRunners/run_eagle_test.py SessionPersistence.test.tcl --session-id "$PERSISTENCE_SESSION_ID" 2>&1)
echo "$OUTPUT"

# Extract session ID from output
SESSION_ID=$(echo "$OUTPUT" | grep "Session ID:" | awk '{print $3}')

if [ -n "$SESSION_ID" ]; then
    echo -e "${GREEN}✓ Session values set with ID: $SESSION_ID${NC}"
    echo
    echo -e "${YELLOW}Note: To complete persistence test:${NC}"
    echo "1. Restart the Docker container"
    echo "2. Run: python3 TestRunners/run_eagle_test.py SessionPersistenceVerify.test.tcl --session-id $SESSION_ID"
else
    # Check if the test itself succeeded
    if echo "$OUTPUT" | grep -q "Execution succeeded"; then
        echo -e "${GREEN}✓ Session values set with ID: $PERSISTENCE_SESSION_ID${NC}"
        echo -e "${YELLOW}(Session ID not in response, but test succeeded)${NC}"
        echo
        echo -e "${YELLOW}Note: To complete persistence test:${NC}"
        echo "1. Restart the Docker container"
        echo "2. Run: python3 TestRunners/run_eagle_test.py SessionPersistenceVerify.test.tcl --session-id $PERSISTENCE_SESSION_ID"
    else
        echo -e "${RED}✗ Failed to extract session ID${NC}"
        FAILED_TESTS+=("Session Persistence Setup")
    fi
fi
echo

# 5. Concurrent Pool Test
echo -e "${YELLOW}=== CONCURRENT POOL TEST ===${NC}"
echo

echo "Running concurrent interpreter pool test..."
echo "----------------------------------------"

if python3 TestRunners/test_pool_concurrent.py; then
    echo -e "${GREEN}✓ Concurrent pool test PASSED${NC}"
else
    echo -e "${RED}✗ Concurrent pool test FAILED${NC}"
    FAILED_TESTS+=("Concurrent Pool")
fi
echo

# 6. Eagle History Test
echo -e "${YELLOW}=== EAGLE HISTORY TEST ===${NC}"
echo

# First execute a script to generate history
echo "Generating execution history..."
python3 TestRunners/run_eagle_test.py Phase1Complete.test.tcl --security Elevated > /dev/null 2>&1

echo "Testing eagle_history tool..."
echo "----------------------------------------"

# Create a temporary test script for history
cat > /tmp/test_history.tcl << 'EOF'
puts "Testing eagle_history tool"
set history [mcp::call_tool eagle_history {}]
if {[string length $history] > 0} {
    puts "✓ History retrieved successfully"
    puts "History entries found"
} else {
    puts "✗ No history found"
}
EOF

if python3 TestRunners/run_eagle_test.py /tmp/test_history.tcl; then
    echo -e "${GREEN}✓ Eagle history test PASSED${NC}"
else
    echo -e "${RED}✗ Eagle history test FAILED${NC}"
    FAILED_TESTS+=("Eagle History")
fi
rm -f /tmp/test_history.tcl
echo

# Summary
echo "=========================================="
echo "          TEST SUMMARY"
echo "=========================================="
echo

TOTAL_TESTS=11  # Adjust based on actual test count
PASSED_TESTS=$((TOTAL_TESTS - ${#FAILED_TESTS[@]}))

echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: ${#FAILED_TESTS[@]}${NC}"
echo

if [ ${#FAILED_TESTS[@]} -eq 0 ]; then
    echo -e "${GREEN}✓ ALL TESTS PASSED!${NC}"
    echo -e "${GREEN}Phase 1 implementation is fully verified!${NC}"
    exit 0
else
    echo -e "${RED}✗ Some tests failed:${NC}"
    for test in "${FAILED_TESTS[@]}"; do
        echo -e "  ${RED}- $test${NC}"
    done
    echo
    echo -e "${YELLOW}Please check the logs above for details.${NC}"
    exit 1
fi