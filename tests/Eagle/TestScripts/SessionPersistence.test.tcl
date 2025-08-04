# Test script for session persistence
# This script sets session values that should persist across container restarts

puts "Setting Session Values for Persistence Test"
puts "=========================================="

# Set various session values
puts "\nSetting test values in session..."

# String value
mcp::session set persistence_test_string "This value should persist across restarts"
puts "✓ Set string value"

# Numeric value
mcp::session set persistence_test_number 42
puts "✓ Set numeric value"

# List value
mcp::session set persistence_test_list [list apple banana cherry date elderberry]
puts "✓ Set list value"

# Complex value (simulated dict as list)
mcp::session set persistence_test_complex [list name "John Doe" age 30 active true timestamp [clock seconds]]
puts "✓ Set complex value"

# Timestamp for verification
set currentTime [clock format [clock seconds] -format "%Y-%m-%d %H:%M:%S"]
mcp::session set persistence_test_timestamp $currentTime
puts "✓ Set timestamp: $currentTime"

# List all session values
puts "\nCurrent session contents:"
set allKeys [mcp::session list]
foreach key $allKeys {
    set value [mcp::session get $key]
    puts "  $key = $value"
}

puts "\n=========================================="
puts "Session values have been set."
puts "To test persistence:"
puts "1. Note the session ID from the execution result"
puts "2. Restart the Docker container"
puts "3. Run SessionPersistenceVerify.test.tcl with the same session ID"