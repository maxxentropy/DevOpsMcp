# Debug context values
puts "Debugging Context Values"
puts "======================="

# Check environment.isProduction
set isProd [mcp::context get environment.isProduction]
puts "environment.isProduction = '$isProd'"
puts "Is numeric: [string is double -strict $isProd]"
puts "Length: [string length $isProd]"

# Check if it's empty
if {$isProd eq ""} {
    puts "WARNING: isProduction is empty!"
}

# Check all environment values
puts "\nAll environment context values:"
foreach key {type isProduction isDevelopment isStaging} {
    set val [mcp::context get environment.$key]
    puts "  environment.$key = '$val'"
}

# Check boolean handling
puts "\nBoolean value tests:"
puts "isProd eq 'true': [expr {$isProd eq "true"}]"
puts "isProd eq 'false': [expr {$isProd eq "false"}]"
puts "isProd eq '0': [expr {$isProd eq "0"}]"
puts "isProd eq '1': [expr {$isProd eq "1"}]"