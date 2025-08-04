# Eagle Quick Reference Guide

## Most Common Commands

### Variables & Data
```tcl
set var value              # Set variable
set var                    # Get variable value
unset var                  # Delete variable
incr var ?amount?          # Increment (default: 1)
append var string          # Append to variable

# Arrays
set arr(key) value         # Set array element
array exists arr           # Check if array exists
array get arr              # Get all key-value pairs
array names arr            # Get all keys
array size arr             # Number of elements
```

### Lists
```tcl
list a b c                 # Create list
lindex $list 0             # Get element at index
llength $list              # List length
lappend var a b c          # Append to list variable
linsert $list 2 x y        # Insert at position
lrange $list 1 3           # Get sublist
lsearch $list pattern      # Find in list
lsort $list                # Sort list
```

### Strings
```tcl
string length $str         # String length
string index $str 5        # Character at position
string range $str 2 5      # Substring
string match pat* $str     # Pattern match
string trim $str           # Remove whitespace
string tolower $str        # Lowercase
string map {a A} $str      # Character substitution
split $str ,               # Split by delimiter
join $list ,               # Join with delimiter
```

### Control Flow
```tcl
if {$x > 0} {
    # positive
} elseif {$x < 0} {
    # negative  
} else {
    # zero
}

while {$i < 10} {
    incr i
}

foreach item $list {
    puts $item
}

switch $value {
    a { puts "option a" }
    b { puts "option b" }
    default { puts "other" }
}
```

### Procedures
```tcl
proc name {arg1 arg2} {
    return [expr {$arg1 + $arg2}]
}

proc varargs {args} {
    foreach arg $args {
        puts $arg
    }
}
```

### File I/O
```tcl
set fp [open "file.txt" r]
set content [read $fp]
close $fp

# Write file
set fp [open "out.txt" w]
puts $fp "content"
close $fp

# Read line by line
set fp [open "file.txt" r]
while {[gets $fp line] >= 0} {
    puts $line
}
close $fp
```

### Error Handling
```tcl
if {[catch {risky_operation} result]} {
    puts "Error: $result"
} else {
    puts "Success: $result"
}

try {
    risky_operation
} on error {msg} {
    puts "Caught: $msg"
} finally {
    cleanup
}
```

## .NET Integration Cheat Sheet

### Object Creation & Methods
```tcl
# Load assembly
object load System.Windows.Forms

# Create object
set obj [object create System.DateTime]
set obj [object create -alias ArrayList]

# Call methods
$obj Add "item"
set count [$obj Count]

# Static methods
set max [object invoke System.Math Max 5 10]

# Properties
$form Text "Title"
set text [$form Text]
```

### Common .NET Operations
```tcl
# Collections
set list [object create System.Collections.ArrayList]
$list Add "item"
$list RemoveAt 0
$list Clear

# File operations
set file [object create System.IO.FileInfo "test.txt"]
set exists [$file Exists]
set length [$file Length]

# DateTime
set now [object invoke System.DateTime Now]
set year [$now Year]
set formatted [$now ToString "yyyy-MM-dd"]
```

## Expression Functions

### Math
```tcl
expr {abs(-5)}             # 5
expr {min(1,2,3)}          # 1
expr {max(1,2,3)}          # 3
expr {round(3.7)}          # 4
expr {sqrt(16)}            # 4.0
expr {pow(2,3)}            # 8.0
```

### Trigonometry
```tcl
expr {sin($x)}
expr {cos($x)}
expr {atan2($y,$x)}
```

### Type Checking
```tcl
string is integer $val
string is double $val
string is boolean $val
string is list $val
```

## Common Patterns

### Default Values
```tcl
# Method 1: info exists
if {![info exists var]} {
    set var "default"
}

# Method 2: catch
catch {set var} result
if {$result eq ""} {
    set var "default"
}
```

### List Building
```tcl
set result [list]
foreach item $input {
    if {[some_test $item]} {
        lappend result $item
    }
}
```

### Dictionary Pattern
```tcl
array set dict {
    name "John"
    age 30
    city "NYC"
}
puts $dict(name)
```

### Namespace Usage
```tcl
namespace eval ::myapp {
    variable state "initial"
    
    proc doSomething {} {
        variable state
        set state "modified"
    }
}
```

## Debugging Commands

```tcl
info commands pattern      # List commands
info vars pattern         # List variables
info exists var           # Check if exists
info level               # Call stack depth
info frame               # Current frame
info script              # Current script
info nameofexecutable    # Interpreter path
```

## Performance Tips

1. **Use braces in expr**: `expr {$a + $b}` not `expr $a + $b`
2. **Use lappend for lists**: Not `set list "$list $item"`
3. **Compile regular expressions**: Store in variables
4. **Avoid unnecessary substitutions**: Use braces when possible
5. **Use appropriate data structures**: Arrays for lookups, lists for sequences

## Common Mistakes to Avoid

1. **Missing braces in expr**: Always use `expr {expression}`
2. **String operations on lists**: Use list commands
3. **Not closing files**: Always close opened files
4. **Variable name typos**: Eagle won't warn about undefined vars
5. **Forgetting global/upvar**: In procedures accessing outer variables

## Essential Packages

```tcl
package require Eagle.Library     # Core utilities
package require Eagle.Test        # Testing framework
package require Eagle.Database    # Database access
package require Eagle.Platform    # Platform utilities
package require Eagle.Safe        # Safe interpreters
```

## Command-Line Options

```bash
EagleShell.exe script.eagle      # Run script
EagleShell.exe -safe script.eagle # Safe mode
EagleShell.exe -file script.eagle # Explicit file
EagleShell.exe                   # Interactive mode
```

---

Remember: Eagle = Tcl syntax + .NET power!