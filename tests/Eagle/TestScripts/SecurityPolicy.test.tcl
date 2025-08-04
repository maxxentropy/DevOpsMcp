# Test script for security policy enforcement
# Phase 1 implementation verification

puts "Testing Security Policy Enforcement"
puts "==================================="

# Test 1: Safe operations (should work at all security levels)
puts "\nTest 1: Safe Operations"
set result [expr 2 + 2]
puts "Math operation: 2 + 2 = $result"

set list [list a b c d]
puts "List operation: $list"

# Test 2: File operations (should be restricted at higher security levels)
puts "\nTest 2: File Operations"
set canWrite 0
catch {
    # Try to write a file
    set fp [open "/tmp/test-security.txt" w]
    puts $fp "Security test"
    close $fp
    set canWrite 1
    puts "✓ File write succeeded"
} err
if {!$canWrite} {
    puts "✗ File write blocked: $err"
}

set canRead 0
catch {
    # Try to read a file
    set fp [open "/etc/passwd" r]
    close $fp
    set canRead 1
    puts "✓ File read succeeded"
} err
if {!$canRead} {
    puts "✗ File read blocked: $err"
}

# Test 3: Command execution (should be restricted)
puts "\nTest 3: Command Execution"
set canExec 0
catch {
    # Try to execute a system command
    exec ls /tmp
    set canExec 1
    puts "✓ Command execution succeeded"
} err
if {!$canExec} {
    puts "✗ Command execution blocked: $err"
}

# Test 4: Network operations (should be restricted)
puts "\nTest 4: Network Operations"
set canSocket 0
catch {
    # Try to create a socket
    socket google.com 80
    set canSocket 1
    puts "✓ Socket creation succeeded"
} err
if {!$canSocket} {
    puts "✗ Socket creation blocked: $err"
}

# Test 5: Environment variable access
puts "\nTest 5: Environment Variables"
set canEnv 0
catch {
    # Try to access environment variables
    set path $env(PATH)
    set canEnv 1
    puts "✓ Environment access succeeded"
} err
if {!$canEnv} {
    puts "✗ Environment access blocked: $err"
}

# Test 6: Interpreter creation (should be restricted)
puts "\nTest 6: Interpreter Creation"
set canInterp 0
catch {
    # Try to create a new interpreter
    interp create child
    set canInterp 1
    puts "✓ Interpreter creation succeeded"
} err
if {!$canInterp} {
    puts "✗ Interpreter creation blocked: $err"
}

# Summary
puts "\n==================================="
puts "Security Policy Test Complete"
puts "Operations allowed:"
if {$canWrite} { puts "  - File write" }
if {$canRead} { puts "  - File read" }
if {$canExec} { puts "  - Command execution" }
if {$canSocket} { puts "  - Socket creation" }
if {$canEnv} { puts "  - Environment access" }
if {$canInterp} { puts "  - Interpreter creation" }

set totalAllowed [expr {$canWrite + $canRead + $canExec + $canSocket + $canEnv + $canInterp}]
puts "\nTotal operations allowed: $totalAllowed/6"