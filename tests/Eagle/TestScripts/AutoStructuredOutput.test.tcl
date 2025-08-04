# Test automatic structured output detection
# This tests that Tcl lists and dictionaries are automatically converted to JSON

puts "Testing Automatic Structured Output Detection"
puts "==========================================="
puts ""

# Test 1: Return a simple list
puts "Test 1: Simple List"
set simple_list [list apple banana cherry date elderberry]
puts "Returning Tcl list: $simple_list"
puts ""

# Return the list as the last expression
# The executor should detect this and convert to JSON automatically
return $simple_list