# Test script for verifying session persistence
# This script checks if session values persisted across container restart

puts "Verifying Session Persistence"
puts "============================="

# Check if we can retrieve the persisted values
puts "\nChecking persisted values..."

set passed 0
set failed 0

# Check string value
set stringValue [mcp::session get persistence_test_string]
if {$stringValue eq "This value should persist across restarts"} {
    puts "✓ String value persisted correctly"
    incr passed
} else {
    puts "✗ String value not found or incorrect: '$stringValue'"
    incr failed
}

# Check numeric value
set numberValue [mcp::session get persistence_test_number]
if {$numberValue eq "42"} {
    puts "✓ Numeric value persisted correctly"
    incr passed
} else {
    puts "✗ Numeric value not found or incorrect: '$numberValue'"
    incr failed
}

# Check list value
set listValue [mcp::session get persistence_test_list]
if {$listValue eq "apple banana cherry date elderberry"} {
    puts "✓ List value persisted correctly"
    incr passed
} else {
    puts "✗ List value not found or incorrect: '$listValue'"
    incr failed
}

# Check complex value
set complexValue [mcp::session get persistence_test_complex]
if {[string match "*John Doe*" $complexValue] && [string match "*age 30*" $complexValue]} {
    puts "✓ Complex value persisted correctly"
    incr passed
} else {
    puts "✗ Complex value not found or incorrect: '$complexValue'"
    incr failed
}

# Check timestamp
set timestampValue [mcp::session get persistence_test_timestamp]
if {$timestampValue ne ""} {
    puts "✓ Timestamp persisted: $timestampValue"
    incr passed
} else {
    puts "✗ Timestamp not found"
    incr failed
}

# List all session values
puts "\nAll session contents:"
set allKeys [mcp::session list]
foreach key $allKeys {
    set value [mcp::session get $key]
    puts "  $key = $value"
}

puts "\n============================="
puts "Persistence Test Results: $passed passed, $failed failed"

if {$failed == 0} {
    puts "SUCCESS: All session values persisted correctly!"
} else {
    puts "FAILURE: Some session values were lost"
}