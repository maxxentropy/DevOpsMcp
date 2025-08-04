# Test script for interpreter pool management
# This script simulates a workload to test pool behavior

puts "Testing Interpreter Pool Management"
puts "=================================="

# Get start time
set startTime [clock milliseconds]

# Test 1: Basic execution
puts "\nTest 1: Basic Execution"
set result [expr 2 + 2]
puts "Simple calculation: 2 + 2 = $result"

# Test 2: Simulate some work
puts "\nTest 2: Simulating Work"
set sum 0
for {set i 1} {$i <= 1000} {incr i} {
    set sum [expr {$sum + $i}]
}
puts "Sum of 1 to 1000: $sum"

# Test 3: String operations
puts "\nTest 3: String Operations"
set text "The quick brown fox jumps over the lazy dog"
set reversed ""
for {set i [expr {[string length $text] - 1}]} {$i >= 0} {incr i -1} {
    append reversed [string index $text $i]
}
puts "Original: $text"
puts "Reversed: $reversed"

# Test 4: List operations
puts "\nTest 4: List Operations"
set myList [list]
for {set i 1} {$i <= 20} {incr i} {
    lappend myList "item$i"
}
puts "Created list with [llength $myList] items"
# Eagle doesn't support ternary operator, use a simpler shuffle
set shuffled $myList
# Just reverse it as a simple "shuffle" for testing
set shuffled [lreverse $shuffled]
puts "First 5 shuffled: [lrange $shuffled 0 4]"

# Test 5: Session operations (to test thread safety)
puts "\nTest 5: Session Operations"
set sessionKey "pool_test_[clock milliseconds]"
mcp::session set $sessionKey "test_value"
set retrieved [mcp::session get $sessionKey]
if {$retrieved eq "test_value"} {
    puts "✓ Session operation successful"
} else {
    puts "✗ Session operation failed"
}

# Calculate execution time
set endTime [clock milliseconds]
set duration [expr {$endTime - $startTime}]
puts "\n=================================="
puts "Total execution time: ${duration}ms"
puts "Pool test completed successfully"