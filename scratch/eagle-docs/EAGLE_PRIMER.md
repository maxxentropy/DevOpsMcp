# The Complete Eagle Scripting Language Primer
## A Comprehensive Guide for AI Agents and Developers

---

## Table of Contents

1. [Introduction to Eagle](#introduction-to-eagle)
2. [Language Fundamentals](#language-fundamentals)
3. [Core Commands Reference](#core-commands-reference)
4. [Mathematical Functions](#mathematical-functions)
5. [.NET Integration](#net-integration)
6. [Advanced Features](#advanced-features)
7. [Best Practices](#best-practices)
8. [Common Patterns](#common-patterns)
9. [Debugging and Development](#debugging-and-development)
10. [Package System](#package-system)
11. [Advanced Testing Framework](#advanced-testing-framework)

---

## Introduction to Eagle

**Eagle** (Extensible Adaptable Generalized Logic Engine) is an implementation of the Tcl scripting language for the Common Language Runtime (CLR), written entirely in C#. It provides most of Tcl 8.4's functionality while borrowing features from Tcl 8.5/8.6 and adding powerful .NET integration capabilities.

### Key Characteristics:
- **Tcl-Compatible**: Implements core Tcl syntax and commands
- **CLR-Native**: Deep integration with .NET Framework
- **Extensible**: Easy to extend with custom commands and functions
- **Safe**: Built-in security features and sandboxing capabilities
- **Cross-Platform**: Runs on Windows, Linux, and macOS via .NET/Mono

### When to Use Eagle:
- Embedding a scripting engine in .NET applications
- Automating .NET applications with scripts
- Creating cross-platform scripts that leverage .NET libraries
- Building test harnesses for .NET code
- Rapid prototyping with full .NET access

---

## Language Fundamentals

### Basic Syntax

Eagle follows Tcl's syntax rules:

```tcl
# Comments start with #
set variable "value"              ; # Variable assignment
puts $variable                    ; # Variable substitution with $
puts "Hello $variable"            ; # String interpolation
puts {literal $variable}          ; # Literal strings with {}
set result [expr 2 + 2]          ; # Command substitution with []
```

### Variable Types

Eagle is dynamically typed like Tcl:

```tcl
set x 42                         ; # Integer
set y 3.14                       ; # Floating point
set s "hello world"              ; # String
set list {a b c}                 ; # List
set bool true                    ; # Boolean
```

### Arrays

Arrays in Eagle are associative (hash tables):

```tcl
set arr(name) "John"
set arr(age) 30
set arr(1) "first"
puts $arr(name)                  ; # John

# Array operations
array exists arr                 ; # 1 (true)
array size arr                   ; # 3
array names arr                  ; # name age 1
array get arr                    ; # name John age 30 1 first
```

### Control Structures

```tcl
# If statement
if {$x > 10} {
    puts "x is greater than 10"
} elseif {$x < 10} {
    puts "x is less than 10"
} else {
    puts "x equals 10"
}

# While loop
set i 0
while {$i < 5} {
    puts $i
    incr i
}

# For loop
for {set i 0} {$i < 5} {incr i} {
    puts $i
}

# Foreach loop
foreach item {a b c d} {
    puts $item
}

# Switch statement
switch $value {
    "apple" { puts "fruit" }
    "carrot" { puts "vegetable" }
    default { puts "unknown" }
}
```

### Procedures

```tcl
# Basic procedure
proc greet {name} {
    return "Hello, $name!"
}

# Procedure with default arguments
proc multiply {a {b 1}} {
    return [expr {$a * $b}]
}

# Procedure with variable arguments
proc sum {args} {
    set total 0
    foreach num $args {
        incr total $num
    }
    return $total
}
```

---

## Core Commands Reference

### Essential Commands

#### Variable Management
- **set** varName ?value? - Set or get variable value
- **unset** ?-nocomplain? varName - Delete variable
- **incr** varName ?increment? - Increment numeric variable
- **append** varName ?value ...? - Append to string variable
- **global** varName - Access global variable
- **upvar** level varName localName - Create variable alias
- **variable** ?name value? - Declare namespace variable

#### String Operations
- **string length** string - Get string length
- **string index** string charIndex - Get character at index
- **string range** string first last - Extract substring
- **string compare** ?options? string1 string2 - Compare strings
- **string match** ?-nocase? pattern string - Pattern matching
- **string trim** ?chars? string - Remove leading/trailing characters
- **string tolower** string - Convert to lowercase
- **string toupper** string - Convert to uppercase
- **string replace** string first last ?newstring? - Replace substring
- **format** formatString ?arg ...? - Printf-style formatting
- **split** string ?splitChars? - Split string into list
- **join** list ?joinString? - Join list into string

#### List Operations
- **list** ?arg ...? - Create a list
- **lindex** list ?index ...? - Get list element
- **llength** list - Get list length
- **lappend** varName ?value ...? - Append to list variable
- **linsert** list index ?element ...? - Insert into list
- **lreplace** list first last ?element ...? - Replace list elements
- **lsearch** ?options? list pattern - Search in list
- **lsort** ?options? list - Sort list
- **lrange** list first last - Extract sublist
- **lreverse** list - Reverse list
- **lmap** varname list ?varname list ...? body - Map over lists

#### File Operations
- **open** fileName ?access? ?permissions? - Open file
- **close** channelId - Close file
- **read** channelId ?numChars? - Read from file
- **puts** ?-nonewline? ?channelId? string - Write to file
- **gets** channelId ?varName? - Read line from file
- **eof** channelId - Check end of file
- **flush** channelId - Flush output buffer
- **file exists** name - Check file existence
- **file size** name - Get file size
- **file dirname** name - Get directory name
- **file join** name ?name ...? - Join path components
- **file normalize** name - Normalize path
- **cd** ?dirName? - Change directory
- **pwd** - Get current directory
- **glob** ?options? pattern ?pattern ...? - File pattern matching

#### Control Flow
- **if** expr1 ?then? body1 elseif expr2 ?then? body2 ... ?else? bodyN
- **while** test body
- **for** start test next body
- **foreach** varname list ?varname list ...? body
- **switch** ?options? string {pattern body ...}
- **break** - Exit loop
- **continue** - Continue to next iteration
- **return** ?-code code? ?-level level? ?string?
- **error** message ?info? ?code?
- **catch** script ?resultVarName? ?optionsVarName?
- **try** body ?handler ...?
- **throw** ?type? message

#### Expression Evaluation
- **expr** arg ?arg ...? - Evaluate mathematical expression
- Operators: + - * / % ** == != < > <= >= && || ! & | ^ ~ << >>
- Functions: See [Mathematical Functions](#mathematical-functions)

---

## Mathematical Functions

Eagle provides comprehensive mathematical functions through the expr command:

### Arithmetic Functions
- **abs(x)** - Absolute value
- **ceil(x)** - Ceiling function
- **floor(x)** - Floor function
- **round(x)** - Round to nearest integer
- **truncate(x)** - Truncate to integer
- **sign(x)** - Sign of number (-1, 0, or 1)
- **min(x,y,...)** - Minimum value
- **max(x,y,...)** - Maximum value
- **fmod(x,y)** - Floating-point remainder

### Trigonometric Functions
- **sin(x)** - Sine (radians)
- **cos(x)** - Cosine (radians)
- **tan(x)** - Tangent (radians)
- **asin(x)** - Arc sine
- **acos(x)** - Arc cosine
- **atan(x)** - Arc tangent
- **atan2(y,x)** - Two-argument arc tangent
- **sinh(x)** - Hyperbolic sine
- **cosh(x)** - Hyperbolic cosine
- **tanh(x)** - Hyperbolic tangent

### Logarithmic & Exponential
- **exp(x)** - e raised to power x
- **log(x)** - Natural logarithm
- **log10(x)** - Base-10 logarithm
- **log2(x)** - Base-2 logarithm
- **pow(x,y)** - x raised to power y
- **sqrt(x)** - Square root
- **hypot(x,y)** - Euclidean distance

### Type Checking Functions
- **isfinite(x)** - Check if finite
- **isinf(x)** - Check if infinite
- **isnan(x)** - Check if NaN
- **isnormal(x)** - Check if normal
- **issubnormal(x)** - Check if subnormal
- **isunordered(x,y)** - Check if unordered

### Type Conversion Functions
- **bool(x)** - Convert to boolean
- **int(x)** - Convert to integer
- **double(x)** - Convert to double
- **wide(x)** - Convert to wide integer
- **decimal(x)** - Convert to decimal
- **typeof(x)** - Get type name

### Random Number Functions
- **rand()** - Random number [0,1)
- **random(max)** - Random integer [0,max)
- **srand(seed)** - Seed random generator
- **randstr(?length? ?chars?)** - Random string

### Constants
- **pi()** - π (3.14159...)
- **e()** - Euler's number (2.71828...)
- **epsilon()** - Machine epsilon

---

## .NET Integration

Eagle's most powerful feature is its seamless .NET integration:

### Loading Assemblies

```tcl
# Load .NET assembly
object load System.Windows.Forms
object load -import System.Data

# Load from file
object load -file MyAssembly.dll
```

### Creating Objects

```tcl
# Create .NET object
set list [object create System.Collections.ArrayList]
set form [object create -alias System.Windows.Forms.Form]

# With constructor arguments
set file [object create System.IO.FileInfo "C:/test.txt"]
```

### Calling Methods

```tcl
# Instance methods
$list Add "item1"
$list Add "item2"
set count [$list Count]

# Static methods
set result [object invoke System.Math Max 10 20]

# Properties
$form Text "My Window"
set title [$form Text]

# Indexers
set item [$list Item 0]
```

### Event Handling

```tcl
# Define event handler
proc button_Click {sender e} {
    puts "Button clicked!"
}

# Attach to event
$button add_Click button_Click

# Detach from event
$button remove_Click button_Click
```

### Type Conversion

```tcl
# Convert Eagle values to .NET types
set netString [object invoke System.String new [split "hello"]]
set netInt [object invoke System.Int32 Parse "42"]

# Convert .NET objects to Eagle values
set eagleString [object toString $netObject]
set eagleValue [object invoke $netObject ToString]
```

### Working with Generics

```tcl
# Create generic types
set list [object create "System.Collections.Generic.List`1\[System.String\]"]
$list Add "test"

# Dictionary example
set dict [object create \
    "System.Collections.Generic.Dictionary`2\[System.String,System.Int32\]"]
$dict Add "key" 42
```

### LINQ Integration

```tcl
# Load LINQ
object load -import System.Linq

# Use LINQ methods
set numbers [object create "System.Int32\[\]" 5]
$numbers SetValue 1 0
$numbers SetValue 2 1
$numbers SetValue 3 2
$numbers SetValue 4 3
$numbers SetValue 5 4

set sum [object invoke System.Linq.Enumerable Sum $numbers]
```

---

## Advanced Features

### Namespaces

```tcl
# Create namespace
namespace eval ::myapp {
    variable counter 0
    
    proc increment {} {
        variable counter
        incr counter
    }
}

# Use namespace
::myapp::increment
puts $::myapp::counter
```

### Object-Oriented Programming

```tcl
# Define a class-like object
object create MyClass {
    # Constructor
    proc constructor {args} {
        variable name
        set name [lindex $args 0]
    }
    
    # Method
    proc greet {} {
        variable name
        return "Hello, $name!"
    }
}

# Create instance
set obj [object create MyClass "John"]
puts [$obj greet]
```

### Asynchronous Operations

```tcl
# Schedule delayed execution
after 1000 {puts "One second later"}

# Schedule with event ID
set id [after 2000 myProc arg1 arg2]
after cancel $id

# Idle callback
after idle {puts "When idle"}
```

### Debugging Features

```tcl
# Enable debugging
debug enable

# Set breakpoints
debug break -location "file.eagle:10"

# Watch variables
debug watch -variable myVar

# Step through code
debug step
debug continue

# Examine call stack
info frame
info level
```

### Native Tcl Integration

```tcl
# Load native Tcl
tcl load

# Execute Tcl code
tcl eval {
    package require Tk
    button .b -text "Click me"
    pack .b
}

# Transfer data between Eagle and Tcl
tcl set myvar "value"
set eagleVar [tcl get myvar]
```

---

## Best Practices

### 1. Error Handling

Always use proper error handling:

```tcl
if {[catch {
    # Risky operation
    set file [open "data.txt" r]
    set content [read $file]
    close $file
} result options]} {
    puts "Error: $result"
    # Handle error appropriately
} else {
    # Process content
    puts "File content: $result"
}
```

### 2. Resource Management

Always clean up resources:

```tcl
proc processFile {filename} {
    set file [open $filename r]
    try {
        return [read $file]
    } finally {
        close $file
    }
}
```

### 3. Performance Optimization

```tcl
# Use compiled expressions
set result [expr {$a + $b * $c}]  ; # Good - compiled
set result [expr $a + $b * $c]    ; # Bad - interpreted

# Build lists efficiently
set list [list]
lappend list a b c d              ; # Good
set list "$list $item"            ; # Bad for lists

# Use appropriate data structures
# Arrays for key-value pairs
# Lists for ordered collections
```

### 4. Code Organization

```tcl
# Use namespaces for modularity
namespace eval ::myapp::utils {
    proc helper {args} {
        # Implementation
    }
}

# Use packages for reusable code
package provide MyPackage 1.0
package require MyPackage
```

### 5. Security Considerations

```tcl
# Use safe interpreters for untrusted code
set safeInterp [interp create -safe]
$safeInterp eval $untrustedScript
interp delete $safeInterp

# Validate input
if {![string is integer -strict $userInput]} {
    error "Invalid input: expected integer"
}
```

---

## Common Patterns

### Configuration Files

```tcl
# config.eagle
set config(server) "localhost"
set config(port) 8080
set config(debug) true

# main.eagle
source config.eagle
puts "Connecting to $config(server):$config(port)"
```

### Plugin System

```tcl
# Plugin interface
proc loadPlugin {name} {
    set pluginFile [file join plugins ${name}.eagle]
    if {[file exists $pluginFile]} {
        namespace eval ::plugins::$name {
            source $pluginFile
        }
        return true
    }
    return false
}

# Load all plugins
foreach plugin [glob -nocomplain plugins/*.eagle] {
    loadPlugin [file rootname [file tail $plugin]]
}
```

### Event-Driven Programming

```tcl
# Event dispatcher
namespace eval ::events {
    variable handlers
    
    proc bind {event handler} {
        variable handlers
        lappend handlers($event) $handler
    }
    
    proc trigger {event args} {
        variable handlers
        if {[info exists handlers($event)]} {
            foreach handler $handlers($event) {
                {*}$handler {*}$args
            }
        }
    }
}

# Usage
::events::bind button.click {puts "Button clicked!"}
::events::trigger button.click
```

### Database Access

```tcl
package require Eagle.Database

# Open database
set db [sql open -type SQLite -data "mydb.db"]

# Execute query
set results [sql execute -execute reader $db \
    "SELECT * FROM users WHERE age > @age" \
    [list @age 18]]

# Process results
while {[$results Read]} {
    puts "User: [$results GetString 1]"
}

# Cleanup
$results Close
sql close $db
```

### GUI Application Template

```tcl
# Load Windows Forms
object load -import System.Windows.Forms

# Create main form
set form [object create -alias Form]
$form Text "Eagle GUI Application"
$form Size [object create System.Drawing.Size 400 300]

# Add controls
set button [object create -alias Button]
$button Text "Click Me"
$button Location [object create System.Drawing.Point 150 100]

# Event handler
proc button_Click {sender e} {
    object invoke System.Windows.Forms.MessageBox Show \
        "Hello from Eagle!" "Message"
}
$button add_Click button_Click

# Add to form
$form.Controls Add $button

# Run application
object invoke System.Windows.Forms.Application Run $form
```

---

## Debugging and Development

### Interactive Development

```tcl
# Start interactive shell
# Run: EagleShell.exe

# Get help
help
help set
help -syntax string

# Explore objects
info commands s*
info vars
info procs

# Time execution
time {complex_operation} 1000
```

### Profiling

```tcl
# Enable profiling
debug profile on

# Run code
source myapp.eagle

# Get profile data
debug profile data
debug profile clear
```

### Testing

```tcl
# Simple test framework
proc test {name script expected} {
    if {[catch {uplevel 1 $script} result]} {
        puts "FAIL $name: Error - $result"
    } elseif {$result ne $expected} {
        puts "FAIL $name: Expected '$expected', got '$result'"
    } else {
        puts "PASS $name"
    }
}

# Run tests
test "addition" {expr 2 + 2} 4
test "string concat" {string cat "Hello" " " "World"} "Hello World"
```

### Logging

```tcl
namespace eval ::logger {
    variable logLevel "INFO"
    variable logFile ""
    
    proc log {level message} {
        variable logLevel
        variable logFile
        
        set timestamp [clock format [clock seconds]]
        set logMessage "$timestamp [$level] $message"
        
        # Console output
        puts stderr $logMessage
        
        # File output
        if {$logFile ne ""} {
            set fp [open $logFile a]
            puts $fp $logMessage
            close $fp
        }
    }
    
    proc debug {msg} { log "DEBUG" $msg }
    proc info {msg} { log "INFO" $msg }
    proc warn {msg} { log "WARN" $msg }
    proc error {msg} { log "ERROR" $msg }
}
```

---

## Package System

### Creating Packages

```tcl
# mypackage.eagle
package provide MyPackage 1.0

namespace eval ::mypackage {
    proc hello {name} {
        return "Hello, $name from MyPackage!"
    }
    
    namespace export hello
}
```

### Package Index

```tcl
# pkgIndex.eagle
package ifneeded MyPackage 1.0 \
    [list source [file join $dir mypackage.eagle]]
```

### Using Packages

```tcl
# Add to package path
lappend auto_path /path/to/packages

# Load package
package require MyPackage 1.0

# Use package
puts [mypackage::hello "World"]
```

### Standard Library Packages

Eagle comes with built-in packages:

- **Eagle.Library** - Core library functions
- **Eagle.Safe** - Safe interpreter support
- **Eagle.Test** - Testing framework
- **Eagle.Database** - Database connectivity
- **Eagle.Platform** - Platform-specific utilities
- **Eagle.Shell** - Shell integration
- **Eagle.Process** - Process management
- **Eagle.File** - Advanced file operations

---

## Quick Reference Card

### Command Invocation Styles
```tcl
cmd arg1 arg2              ; # Simple
cmd -option value arg      ; # With options
[cmd arg1] arg2           ; # Command substitution
{*}$cmdList               ; # Argument expansion
```

### Variable Substitution Rules
```tcl
$var                      ; # Simple variable
${var}                    ; # Variable with explicit boundaries
$var(index)              ; # Array element
${var(complex index)}    ; # Complex array index
$$ptr                    ; # Double substitution
```

### Quoting Mechanisms
```tcl
"double quotes $var"      ; # Allows substitution
{curly braces $var}      ; # Prevents substitution
[command substitution]    ; # Execute and substitute
\n \t \r                 ; # Escape sequences
```

### Common Idioms
```tcl
# Default values
set value [expr {[info exists var] ? $var : "default"}]

# Safe array access
if {[info exists arr($key)]} {
    set value $arr($key)
}

# Building lists
set list [list]
foreach item $source {
    if {[condition $item]} {
        lappend list $item
    }
}

# Dictionary-style operations
array set dict {
    key1 value1
    key2 value2
}
```

---

## Advanced Testing Framework

Eagle includes a comprehensive testing framework with utilities found in `lib/Eagle1.0/test.eagle`. Here are the key patterns and techniques:

### Test Constraints

Constraints allow you to conditionally run tests based on system capabilities:

```tcl
# Check if a constraint exists
if {[haveConstraint "windows"]} {
    puts "Running on Windows"
}

# Add a new constraint
addConstraint "myFeature" [expr {[info commands mycommand] ne ""}]

# Use constraints in tests
runTest {test example-1.0 "Feature test" -constraints {windows myFeature} -body {
    # Test code here
}}
```

### Flexible Procedures

Eagle supports flexible procedure definitions that work in both Eagle and Tcl:

```tcl
# Define a flexible procedure
f_proc myProc {arg1 {arg2 "default"}} {
    return "$arg1 - $arg2"
}

# Stub procedures for development
s_proc futureFeature {args} {
    # Automatically generates stub implementation
}
```

### Advanced Error Handling

The test framework provides sophisticated error handling patterns:

```tcl
# Break on error for debugging
proc breakOnError { script } {
    while {true} {
        set code [catch {uplevel 1 $script} result]
        if {$code == 0} {
            return $result
        }
        puts "Error: $result"
        debug break
        # User can fix the issue and return 1 to retry
        if {![catch {set retry} r] && $r} {
            continue
        }
        error $result
    }
}

# Usage
breakOnError {
    # Code that might fail
    set value $missingVar
}
```

### Test Execution Patterns

```tcl
# Run test with prologue and epilogue
proc runTestPrologue { {overridePath ""} {quiet false} } {
    # Setup test environment
}

proc runTestEpilogue { {overridePath ""} {quiet false} } {
    # Cleanup test environment
}

# Execute multiple test scripts
array set testScripts {
    1 {test basic functionality}
    2 {test advanced features}
}

addTestScripts testScripts {
    test error handling
}

evaluateTestScripts "" testScripts code result
set combined [combineTestScriptResults testScripts code result]
```

### Interactive Testing Utilities

```tcl
# Prompt for user input during testing
set input [promptForAndGetTextInput \
    "Enter test value: " \
    "Test Input" \
    "default value"]

# Test with visual feedback (Windows Forms)
if {[isEagle] && ![info exists ::no(inputBox)]} {
    object load Microsoft.VisualBasic
    set value [object invoke \
        Microsoft.VisualBasic.Interaction \
        InputBox "Prompt" "Title" "Default"]
}
```

### Performance and Resource Testing

```tcl
# Test resource usage
proc testResourceUsage { script } {
    set before [info performance]
    set result [uplevel 1 $script]
    set after [info performance]
    
    # Analyze performance metrics
    foreach key [array names before] {
        set delta [expr {$after($key) - $before($key)}]
        puts "$key: $delta"
    }
    
    return $result
}

# Enable fail-safe exit for long-running tests
enableFailSafeExitAfter 600000  ; # 10 minutes
```

### Process Management for Testing

```tcl
# Get process group (Unix/Linux)
proc getProcessGroup { pid {varName ""} {quiet false} } {
    if {[string length $varName] > 0} {
        upvar 1 $varName pgid
    }
    
    if {[catch {
        string trim [exec -success Success -- ps -o pgid= $pid]
    } pgid] == 0 && [string is integer -strict $pgid]} {
        return true
    }
    return false
}

# Kill process group if needed
proc maybeKillProcessGroup { pid {self false} {quiet false} } {
    if {![isWindows]} {
        if {[getProcessGroup $pid pgid]} {
            exec -success Success -- kill -s KILL -$pgid
        }
    }
}
```

### Debugging and Tracing

```tcl
# Enable maximum tracing
proc enableTracing { {stateTypes "All"} {message true} } {
    debug trace -priority {=Always +NoLimits} \
        -category META_TRACING \
        -resetsystem true -resetlisteners true -forceenabled true \
        -statetypes $stateTypes -default true -log true
}

# Debug hook for test debugging
proc debugBreakHook { args } {
    set name [getNameForDebugHook $args]
    puts "Debug break at: $name"
    debug break
}

# Install debug hook
debug hook -type Before "test-*" ::debugBreakHook
```

### Test Channel Management

```tcl
# Get test output channel
set channel [getTestChannelOrDefault]

# Write to test channel
tputs $channel "Test message\n"

# Test-specific puts command
proc tputs { channel string } {
    if {[isEagle]} {
        # Use Eagle's test output system
        puts -nonewline -test $channel $string
    } else {
        # Use standard Tcl output
        puts -nonewline $channel $string
    }
}
```

### Native Tcl Integration Testing

```tcl
# Test Tcl integration
if {[isEagle]} {
    # Load Tcl for testing
    if {![tcl ready]} {
        tcl load -maybetrustedonly -bridge -robustify \
            -minimumversion 8.6 -maximumversion 8.9
    }
    
    # Execute Tcl code
    tcl eval {
        # Native Tcl code here
        package require Tk
    }
    
    # Transfer data
    tcl set tclVar "value"
    set eagleVar [tcl get tclVar]
}
```

---

## Conclusion

Eagle provides a powerful scripting environment that combines Tcl's elegant syntax with the full power of the .NET Framework. This primer covers the essential aspects needed to become proficient in Eagle scripting.

### Key Takeaways:
1. Eagle is Tcl-compatible but not identical to Tcl
2. .NET integration is Eagle's greatest strength
3. Proper error handling and resource management are crucial
4. The package system enables modular, reusable code
5. Debugging tools are comprehensive and powerful
6. The testing framework provides advanced utilities for robust development

### Next Steps:
- Explore the example scripts in the Eagle distribution
- Read the test suite for advanced usage patterns
- Experiment with .NET integration for your use cases
- Study the test.eagle file for advanced testing patterns
- Join the Eagle community for support and updates

Remember: Eagle is designed to be both powerful and safe. Use its features wisely to create robust, maintainable scripts that leverage the best of both Tcl and .NET worlds.