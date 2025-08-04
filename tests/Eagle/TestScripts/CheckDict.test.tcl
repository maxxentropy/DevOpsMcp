# Check if dict command exists
if {[info commands dict] ne ""} {
    puts "dict command is available"
    # Create a dictionary using dict
    set d [dict create a 1 b 2 c 3]
    puts "Dictionary created: $d"
} else {
    puts "dict command is NOT available"
    # Try creating a key-value list instead
    set kvlist "name {John Doe} age 30 email john@example.com active true"
    puts "Key-value list: $kvlist"
    return $kvlist
}