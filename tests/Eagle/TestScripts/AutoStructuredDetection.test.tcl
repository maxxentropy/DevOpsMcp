# Comprehensive test for automatic structured output detection
puts "Testing Automatic Structured Output Detection"
puts "==========================================="
puts ""

# Test 1: Simple list detection
puts "Test 1: Simple List Detection"
set result1 [list apple banana cherry]
puts "Input: $result1"
set json1 [mcp::output json $result1]
puts "JSON output: $json1"
puts ""

# Test 2: Key-value dictionary detection
puts "Test 2: Dictionary Detection"
set result2 "name Alice age 25 city {New York}"
puts "Input: $result2"
set json2 [mcp::output json $result2]
puts "JSON output: $json2"
puts ""

# Test 3: Nested structure
puts "Test 3: Nested Structure"
set result3 "user {name Bob age 30} settings {theme dark notifications true}"
puts "Input: $result3"
set json3 [mcp::output json $result3]
puts "JSON output: $json3"
puts ""

# Test 4: Return list automatically (should be converted to JSON)
puts "Test 4: Automatic List Conversion on Return"
puts "Returning: [list red green blue yellow]"
puts ""

# This should automatically be converted to JSON
list red green blue yellow