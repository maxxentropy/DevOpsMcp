# Official test script for Eagle Rich Context Injection
# Part of Phase 1.1 implementation

# Test framework using global procedures (Eagle doesn't fully support namespaces)
set testsPassed 0
set testsFailed 0

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

proc assertNotEmpty {value message} {
    assert [expr {[string length $value] > 0}] "$message (value should not be empty)"
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

# Test Suite: Rich Context Injection
puts "Testing Eagle Rich Context Injection (Phase 1.1)"
puts "=========================================="

# Test 1: mcp::context command existence
puts "\nTest Group 1: Context Command Availability"
assert [expr {[info commands mcp::context] ne ""}] "mcp::context command exists"

# Test 2: Basic context retrieval
puts "\nTest Group 2: Context Data Retrieval"
assertNotEmpty [mcp::context get user.name] "Can retrieve user.name"
assertNotEmpty [mcp::context get user.role] "Can retrieve user.role"
assertNotEmpty [mcp::context get project.name] "Can retrieve project.name"
assertNotEmpty [mcp::context get environment.type] "Can retrieve environment.type"

# Test 3: Boolean context values
puts "\nTest Group 3: Boolean Context Values"
set isProd [mcp::context get environment.isProduction]
assert [expr {$isProd eq "true" || $isProd eq "false"}] "environment.isProduction returns boolean string"

# Test 4: Session state management
puts "\nTest Group 4: Session State Management"
assert [expr {[info commands mcp::session] ne ""}] "mcp::session command exists"

# Clear session first
mcp::session clear
set sessionList [mcp::session list]
assert [expr {$sessionList eq "{}" || [llength $sessionList] == 0}] "Session cleared successfully"

# Set and get values
mcp::session set testKey testValue
assertEq [mcp::session get testKey] "testValue" "Can set and get session value"

mcp::session set counter 42
assertEq [mcp::session get counter] "42" "Can store numeric values"

# List keys
set keys [mcp::session list]
assert [expr {[lsearch $keys testKey] >= 0}] "testKey appears in session list"
assert [expr {[lsearch $keys counter] >= 0}] "counter appears in session list"

# Test 5: Tool calling interface
puts "\nTest Group 5: Tool Calling Interface"
assert [expr {[info commands mcp::call_tool] ne ""}] "mcp::call_tool command exists"

# Test tool call (returns placeholder for now)
set result [mcp::call_tool test_tool arg1 arg2]
assertNotEmpty $result "Tool call returns a response"

# Test 6: Error handling
puts "\nTest Group 6: Error Handling"
set errorCaught 0
catch {
    mcp::context invalid_action
} err
assert [expr {$err ne ""}] "Invalid context action throws error"

catch {
    mcp::session invalid_action
} err
assert [expr {$err ne ""}] "Invalid session action throws error"

# Test 7: Context-based logic
puts "\nTest Group 7: Context-Based Conditional Logic"
if {[mcp::context get environment.isProduction] eq "false"} {
    assert 1 "Can use context in conditional logic"
} else {
    assert 0 "Production check failed unexpectedly"
}

# Test 8: Session persistence within execution
puts "\nTest Group 8: Session Persistence"
mcp::session set persistTest "initial"
mcp::session set persistTest "updated"
assertEq [mcp::session get persistTest] "updated" "Session values can be updated"

# Return test result
summary