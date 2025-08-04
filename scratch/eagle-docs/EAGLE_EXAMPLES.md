# Eagle Script Examples

## Basic Examples

### 1. Hello World with Variables
```tcl
#!/usr/bin/env EagleShell

# Basic output and variables
set name "Eagle"
set version 1.0
puts "Hello from $name version $version!"

# String formatting
set msg [format "Welcome to %s %.1f" $name $version]
puts $msg
```

### 2. Working with Lists
```tcl
# Create and manipulate lists
set fruits {apple banana cherry}
lappend fruits "date" "elderberry"

puts "All fruits: $fruits"
puts "First fruit: [lindex $fruits 0]"
puts "Last fruit: [lindex $fruits end]"
puts "Total fruits: [llength $fruits]"

# Iterate over list
foreach fruit $fruits {
    puts "  - $fruit"
}

# List operations
set sorted [lsort $fruits]
set reversed [lreverse $fruits]
set subset [lrange $fruits 1 3]
```

### 3. Array Operations
```tcl
# Create associative array
array set person {
    name "John Doe"
    age 30
    email "john@example.com"
}

# Access array elements
puts "Name: $person(name)"
puts "Age: $person(age)"

# Add new elements
set person(city) "New York"
set person(country) "USA"

# Iterate over array
foreach {key value} [array get person] {
    puts "$key: $value"
}

# Check if key exists
if {[info exists person(phone)]} {
    puts "Phone: $person(phone)"
} else {
    puts "No phone number"
}
```

### 4. File Operations
```tcl
# Write to file
set filename "data.txt"
set fp [open $filename w]
puts $fp "Line 1"
puts $fp "Line 2"
puts $fp "Line 3"
close $fp

# Read entire file
set fp [open $filename r]
set content [read $fp]
close $fp
puts "File content:\n$content"

# Read line by line
set fp [open $filename r]
set lineNum 0
while {[gets $fp line] >= 0} {
    incr lineNum
    puts "$lineNum: $line"
}
close $fp

# File information
if {[file exists $filename]} {
    puts "Size: [file size $filename] bytes"
    puts "Directory: [file dirname $filename]"
    puts "Extension: [file extension $filename]"
}
```

### 5. Procedures and Functions
```tcl
# Simple procedure
proc greet {name} {
    return "Hello, $name!"
}

# Procedure with default arguments
proc power {base {exponent 2}} {
    return [expr {$base ** $exponent}]
}

# Procedure with variable arguments
proc sum {args} {
    set total 0
    foreach num $args {
        set total [expr {$total + $num}]
    }
    return $total
}

# Using procedures
puts [greet "Eagle User"]
puts "2^3 = [power 2 3]"
puts "2^2 = [power 2]"
puts "Sum: [sum 1 2 3 4 5]"

# Procedure with upvar (pass by reference)
proc increment {varName {amount 1}} {
    upvar $varName var
    incr var $amount
}

set counter 10
increment counter
puts "Counter: $counter"
increment counter 5
puts "Counter: $counter"
```

## .NET Integration Examples

### 6. Basic .NET Object Usage
```tcl
# Load System assembly (usually already loaded)
object load mscorlib

# Create DateTime object
set now [object invoke System.DateTime Now]
puts "Current time: [$now ToString]"
puts "Year: [$now Year]"
puts "Month: [$now Month]"
puts "Day: [$now Day]"

# Format date
set formatted [$now ToString "yyyy-MM-dd HH:mm:ss"]
puts "Formatted: $formatted"

# Math operations
set pi [object invoke System.Math PI]
set sqrt2 [object invoke System.Math Sqrt 2]
puts "PI: $pi"
puts "Square root of 2: $sqrt2"
```

### 7. Collections and LINQ
```tcl
# ArrayList
set list [object create System.Collections.ArrayList]
$list Add "Apple"
$list Add "Banana"
$list Add "Cherry"

puts "Count: [$list Count]"
puts "First: [$list Item 0]"

# Convert to array and use LINQ
object load System.Core
set array [$list ToArray]

# Generic List
set stringList [object create "System.Collections.Generic.List`1\[System.String\]"]
$stringList Add "One"
$stringList Add "Two"
$stringList Add "Three"

# Dictionary
set dict [object create \
    "System.Collections.Generic.Dictionary`2\[System.String,System.Int32\]"]
$dict Add "apple" 5
$dict Add "banana" 3
$dict Add "cherry" 10

set appleCount [$dict Item "apple"]
puts "Apples: $appleCount"
```

### 8. GUI Application
```tcl
# Simple Windows Forms application
object load -import System.Windows.Forms
object load -import System.Drawing

# Create form
set form [object create -alias Form]
$form Text "Eagle GUI Demo"
$form Size [object create Size 300 200]
$form StartPosition "CenterScreen"

# Create label
set label [object create -alias Label]
$label Text "Click the button!"
$label Location [object create Point 100 30]
$label AutoSize true

# Create button
set button [object create -alias Button]
$button Text "Click Me"
$button Location [object create Point 110 80]

# Button click handler
proc buttonClick {sender e} {
    global label
    $label Text "Button clicked at [clock format [clock seconds]]"
}

# Wire up event
$button add_Click buttonClick

# Add controls to form
$form.Controls Add $label
$form.Controls Add $button

# Show form (blocking)
$form ShowDialog
```

### 9. File System Operations with .NET
```tcl
# Using System.IO
set dir [object create System.IO.DirectoryInfo "."]

# List files
puts "Files in current directory:"
set files [$dir GetFiles]
foreach file $files {
    puts "  [$file Name] - [$file Length] bytes"
}

# Create and write file
set writer [object create System.IO.StreamWriter "test.txt"]
$writer WriteLine "Hello from Eagle!"
$writer WriteLine "Using .NET IO"
$writer Close

# Read file
set reader [object create System.IO.StreamReader "test.txt"]
while {![$reader EndOfStream]} {
    puts [$reader ReadLine]
}
$reader Close

# File operations
set fileInfo [object create System.IO.FileInfo "test.txt"]
puts "Exists: [$fileInfo Exists]"
puts "Length: [$fileInfo Length]"
puts "Creation: [$fileInfo CreationTime]"
```

### 10. Working with XML
```tcl
object load System.Xml

# Create XML document
set doc [object create System.Xml.XmlDocument]
set root [$doc CreateElement "books"]
$doc AppendChild $root

# Add book
set book [$doc CreateElement "book"]
$book SetAttribute "id" "1"

set title [$doc CreateElement "title"]
$title InnerText "Eagle Programming"
$book AppendChild $title

set author [$doc CreateElement "author"]
$author InnerText "Joe Mistachkin"
$book AppendChild $author

$root AppendChild $book

# Save XML
$doc Save "books.xml"

# Read and parse XML
set doc2 [object create System.Xml.XmlDocument]
$doc2 Load "books.xml"

set nodes [$doc2 SelectNodes "//book"]
puts "Found [$nodes Count] books"

foreach node $nodes {
    set bookTitle [[$node SelectSingleNode "title"] InnerText]
    set bookAuthor [[$node SelectSingleNode "author"] InnerText]
    puts "Book: $bookTitle by $bookAuthor"
}
```

## Advanced Examples

### 11. Asynchronous Operations
```tcl
# Schedule tasks
proc task1 {} {
    puts "[clock seconds]: Task 1 executed"
}

proc task2 {msg} {
    puts "[clock seconds]: Task 2 says: $msg"
}

# Schedule execution
after 1000 task1
after 2000 {task2 "Hello from the future"}
after 3000 {puts "Direct message after 3 seconds"}

# Cancel scheduled task
set taskId [after 5000 {puts "This won't run"}]
after cancel $taskId

# Wait for tasks to complete
after 4000 {set done 1}
vwait done
```

### 12. Error Handling and Debugging
```tcl
# Comprehensive error handling
proc safeDivide {a b} {
    if {$b == 0} {
        error "Division by zero" \
              "MATH DOMAIN {divide by zero}" \
              [list ARITH DIVZERO]
    }
    return [expr {double($a) / $b}]
}

# Try-catch with different error types
try {
    set result [safeDivide 10 0]
} trap {ARITH DIVZERO} {msg} {
    puts "Arithmetic error: $msg"
} on error {msg options} {
    puts "General error: $msg"
    puts "Error code: [dict get $options -errorcode]"
} finally {
    puts "Cleanup code here"
}

# Using catch for error handling
if {[catch {
    set file [open "nonexistent.txt" r]
    set content [read $file]
    close $file
} result options]} {
    puts "Error: $result"
    puts "Error info: [dict get $options -errorinfo]"
}

# Custom error types
proc validateEmail {email} {
    if {![regexp {^[\w\.-]+@[\w\.-]+\.\w+$} $email]} {
        error "Invalid email format" \
              "VALIDATION EMAIL FORMAT" \
              [list VALIDATION EMAIL_FORMAT $email]
    }
    return $email
}
```

### 13. Namespace and Package Example
```tcl
# Define a package
namespace eval ::math {
    namespace export *
    
    variable pi 3.14159265359
    
    proc factorial {n} {
        if {$n <= 1} {
            return 1
        }
        return [expr {$n * [factorial [expr {$n - 1}]]}]
    }
    
    proc fibonacci {n} {
        if {$n <= 1} {
            return $n
        }
        return [expr {[fibonacci [expr {$n-1}]] + [fibonacci [expr {$n-2}]]}]
    }
    
    proc isPrime {n} {
        if {$n <= 1} {return false}
        if {$n <= 3} {return true}
        if {$n % 2 == 0 || $n % 3 == 0} {return false}
        
        for {set i 5} {$i * $i <= $n} {incr i 6} {
            if {$n % $i == 0 || $n % ($i + 2) == 0} {
                return false
            }
        }
        return true
    }
}

# Use the namespace
puts "PI: $::math::pi"
puts "5! = [::math::factorial 5]"
puts "Fibonacci(10) = [::math::fibonacci 10]"

for {set i 1} {$i <= 20} {incr i} {
    if {[::math::isPrime $i]} {
        puts "$i is prime"
    }
}
```

### 14. Regular Expressions
```tcl
# Basic matching
set text "Contact: john.doe@example.com or call 555-123-4567"

# Email extraction
if {[regexp {([\w\.-]+)@([\w\.-]+\.\w+)} $text match email domain]} {
    puts "Email: $email"
    puts "Domain: $domain"
}

# Phone number extraction
if {[regexp {(\d{3})-(\d{3})-(\d{4})} $text match area exchange number]} {
    puts "Phone: ($area) $exchange-$number"
}

# Multiple matches
set urls "Visit http://example.com or https://eagle.com for more info"
set pattern {https?://[\w\.-]+}
set matches [regexp -all -inline $pattern $urls]
puts "Found URLs: $matches"

# String substitution
set result [regsub -all {\d{3}-\d{3}-\d{4}} $text "XXX-XXX-XXXX"]
puts "Masked: $result"

# Case-insensitive matching
set data "Eagle EAGLE eagle"
set count [regsub -all -nocase {eagle} $data "Tcl" result]
puts "Replaced $count occurrences: $result"
```

### 15. Database Operations
```tcl
package require Eagle.Database

# SQLite example
proc databaseExample {} {
    # Create/open database
    set db [sql open -type SQLite -data "test.db"]
    
    try {
        # Create table
        sql execute $db {
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT UNIQUE,
                age INTEGER
            )
        }
        
        # Insert data
        sql execute $db {
            INSERT OR REPLACE INTO users (name, email, age)
            VALUES (@name, @email, @age)
        } [list @name "John Doe" @email "john@example.com" @age 30]
        
        # Query data
        set reader [sql execute -execute reader $db {
            SELECT * FROM users WHERE age > @minAge
        } [list @minAge 25]]
        
        puts "Users over 25:"
        while {[$reader Read]} {
            puts "  ID: [$reader GetInt32 0]"
            puts "  Name: [$reader GetString 1]"
            puts "  Email: [$reader GetString 2]"
            puts "  Age: [$reader GetInt32 3]"
            puts ""
        }
        
        $reader Close
        
    } finally {
        sql close $db
    }
}

# Run example
if {[catch {databaseExample} error]} {
    puts "Database error: $error"
}
```

## Tips for Learning Eagle

1. **Start Simple**: Begin with basic Tcl syntax before diving into .NET integration
2. **Use the REPL**: EagleShell's interactive mode is great for experimentation
3. **Read Error Messages**: Eagle provides detailed error information
4. **Leverage .NET**: Don't recreate what .NET already provides
5. **Check Types**: Use `info type` and `typeof()` to understand object types
6. **Practice Patterns**: Common patterns become intuitive with practice

## Running These Examples

Save any example to a `.eagle` file and run:
```bash
EagleShell.exe example.eagle
```

Or paste directly into the Eagle interactive shell:
```bash
EagleShell.exe
% # paste code here
```