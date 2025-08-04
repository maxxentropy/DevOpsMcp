# Test script for deep context path access
# Phase 1 implementation verification

puts "Testing Deep Context Path Access"
puts "==============================="

# Test 1: Basic context access (existing functionality)
puts "\nTest 1: Basic Context Access"
set userName [mcp::context get user.name]
puts "User name: $userName"

set projectName [mcp::context get project.name]
puts "Project name: $projectName"

# Test 2: Deep path access - project.lastBuild.*
puts "\nTest 2: Deep Path Access - project.lastBuild.*"
set buildStatus [mcp::context get project.lastBuild.status]
puts "Last build status: $buildStatus"

set buildId [mcp::context get project.lastBuild.id]
puts "Last build ID: $buildId"

set buildDate [mcp::context get project.lastBuild.date]
puts "Last build date: $buildDate"

# Test 3: Deep path access - project.repository.*
puts "\nTest 3: Deep Path Access - project.repository.*"
set repoUrl [mcp::context get project.repository.url]
puts "Repository URL: $repoUrl"

set repoBranch [mcp::context get project.repository.branch]
puts "Repository branch: $repoBranch"

# Test 4: Non-existent deep paths
puts "\nTest 4: Non-existent Deep Paths"
set nonExistent [mcp::context get project.lastBuild.nonexistent]
puts "Non-existent path result: '$nonExistent' (should be empty)"

# Test 5: Invalid paths
puts "\nTest 5: Invalid Paths"
set invalidPath [mcp::context get invalid.path.here]
puts "Invalid path result: '$invalidPath' (should be empty)"

# Summary
puts "\n==============================="
puts "Deep Context Path Tests Complete"

# Verify expected values
set passed 0
set failed 0

if {$buildStatus eq "Succeeded"} {
    puts "✓ Build status check passed"
    incr passed
} else {
    puts "✗ Build status check failed"
    incr failed
}

if {$buildId eq "Build-12345"} {
    puts "✓ Build ID check passed"
    incr passed
} else {
    puts "✗ Build ID check failed"
    incr failed
}

if {$repoUrl eq "https://dev.azure.com/org/project/_git/repo"} {
    puts "✓ Repository URL check passed"
    incr passed
} else {
    puts "✗ Repository URL check failed"
    incr failed
}

if {$repoBranch eq "main"} {
    puts "✓ Repository branch check passed"
    incr passed
} else {
    puts "✗ Repository branch check failed"
    incr failed
}

if {$nonExistent eq ""} {
    puts "✓ Non-existent path check passed"
    incr passed
} else {
    puts "✗ Non-existent path check failed"
    incr failed
}

puts "\nTotal: $passed passed, $failed failed"