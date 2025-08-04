# Test automatic structured output detection for dictionaries
# This tests that Tcl dictionaries are automatically converted to JSON

puts "Testing Automatic Dictionary Detection"
puts "====================================="
puts ""

# Test: Return a Tcl dictionary
puts "Creating Tcl dictionary..."
set user_dict [dict create name "John Doe" age 30 email "john@example.com" active true]
puts "Dictionary contents: $user_dict"
puts ""

# Return the dictionary as the last expression
# The executor should detect this and convert to JSON automatically
return $user_dict