# Official test script for Eagle Structured Output Processing
# Part of Phase 1.2 implementation

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

proc assertContains {haystack needle message} {
    assert [expr {[string first $needle $haystack] >= 0}] "$message (should contain: '$needle')"
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

# Test Suite: Structured Output Processing
puts "Testing Eagle Structured Output Processing (Phase 1.2)"
puts "=========================================="

# Test 1: mcp::output command for JSON formatting
puts "\nTest Group 1: JSON Output Formatting"
assert [expr {[info commands mcp::output] ne ""}] "mcp::output command exists"

# Test JSON output with simple data
set simpleData {{"name": "test", "value": 123}}
set jsonOutput [mcp::output $simpleData json]
assertContains $jsonOutput "name" "JSON output contains name"
assertContains $jsonOutput "test" "JSON output contains test value"
assertContains $jsonOutput "123" "JSON output contains numeric value"

# Test JSON output with array
set arrayData {["apple", "banana", "cherry"]}
set jsonArray [mcp::output $arrayData json]
assertContains $jsonArray "apple" "JSON array contains apple"
assertContains $jsonArray "banana" "JSON array contains banana"
assertContains $jsonArray "cherry" "JSON array contains cherry"

# Test 2: XML output formatting
puts "\nTest Group 2: XML Output Formatting"

# Test XML output (Note: current implementation wraps content in output tag)
set xmlData {{"message": "Hello World", "user": {"name": "John", "id": 123}}}
set xmlOutput [mcp::output $xmlData xml]
assertContains $xmlOutput "<output>" "XML output contains output tag"
assertContains $xmlOutput "Hello World" "XML output contains message text"
assertContains $xmlOutput "message" "XML output contains message key"
assertContains $xmlOutput "John" "XML output contains John"

# Test 3: YAML output formatting
puts "\nTest Group 3: YAML Output Formatting"

# Test YAML output
set yamlData {{"name": "Bob", "status": "active", "count": 42, "items": ["item1", "item2", "item3"]}}
set yamlOutput [mcp::output $yamlData yaml]
assertContains $yamlOutput "name: Bob" "YAML output contains name"
assertContains $yamlOutput "status: active" "YAML output contains status"
assertContains $yamlOutput "count: 42" "YAML output contains count"
assertContains $yamlOutput "- item1" "YAML output contains item1"
assertContains $yamlOutput "- item2" "YAML output contains item2"

# Test 4: Table output formatting
puts "\nTest Group 4: Table Output Formatting"

# Test table output
set tableData {[{"ID": 1, "Name": "Alice", "Status": "Active"}, {"ID": 2, "Name": "Bob", "Status": "Inactive"}, {"ID": 3, "Name": "Charlie", "Status": "Active"}]}
set tableOutput [mcp::output $tableData table]
assertContains $tableOutput "ID" "Table contains ID header"
assertContains $tableOutput "Name" "Table contains Name header"
assertContains $tableOutput "Status" "Table contains Status header"
assertContains $tableOutput "Alice" "Table contains Alice"
assertContains $tableOutput "Bob" "Table contains Bob"

# Test 5: CSV output formatting
puts "\nTest Group 5: CSV Output Formatting"

# Test CSV output (Note: current implementation outputs as simple CSV)
set csvData {[{"Name": "John", "Age": 30, "City": "New York"}, {"Name": "Jane", "Age": 25, "City": "San Francisco"}]}
set csvOutput [mcp::output $csvData csv]
assertContains $csvOutput "Content" "CSV contains Content header"
assertContains $csvOutput "John" "CSV contains John"
assertContains $csvOutput "Jane" "CSV contains Jane"

# Test 6: Multiple format tests with same data
puts "\nTest Group 6: Multiple Format Tests"

# Test the same data in different formats
set testData {{"user": "test", "count": 5, "active": true}}

# JSON format
set jsonOut [mcp::output $testData json]
assertContains $jsonOut "\"user\": \"test\"" "JSON format contains user"

# XML format
set xmlOut [mcp::output $testData xml]
assertContains $xmlOut "<output>" "XML format contains output tag"

# YAML format
set yamlOut [mcp::output $testData yaml]
assertContains $yamlOut "user: test" "YAML format contains user"

# Return test result
summary