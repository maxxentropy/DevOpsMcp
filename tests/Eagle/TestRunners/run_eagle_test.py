#!/usr/bin/env python3
"""
Generic Eagle test runner for the DevOps MCP Server
Can run any Eagle test script through the MCP interface
"""

import json
import urllib.request
import sys
import os
from pathlib import Path

def find_test_script(script_name):
    """Find test script in various locations"""
    # If it's already an absolute path and exists, use it
    if os.path.isabs(script_name) and os.path.exists(script_name):
        return script_name
    
    # Search paths
    search_paths = [
        # In container
        f'/app/tests/Eagle/TestScripts/{script_name}',
        # Relative to this script
        Path(__file__).parent.parent / 'TestScripts' / script_name,
        # Project root
        Path(__file__).parent.parent.parent.parent / 'tests/Eagle/TestScripts' / script_name,
        # Current directory
        script_name
    ]
    
    for path in search_paths:
        if os.path.exists(path):
            return str(path)
    
    raise FileNotFoundError(f"Could not find test script: {script_name}")

def run_eagle_test(script_path, session_id=None, security_level="Standard", output_format="plain", env_vars=None, working_dir=None):
    """Run an Eagle test script through the MCP interface"""
    
    # Read the script
    with open(script_path, 'r') as f:
        script_content = f.read()
    
    # Build arguments
    args = {
        "script": script_content,
        "securityLevel": security_level,
        "outputFormat": output_format
    }
    
    if session_id:
        args["sessionId"] = session_id
    
    # Add environment variables if provided
    if env_vars:
        args["environmentVariablesJson"] = json.dumps(env_vars)
    else:
        # Default environment variables for tests
        args["environmentVariablesJson"] = json.dumps({
            "TEST_VAR": "test_value_123"
        })
    
    # Set working directory
    if working_dir:
        args["workingDirectory"] = working_dir
    else:
        # Default to /tmp for tests
        args["workingDirectory"] = "/tmp"
    
    # Create the request
    request = {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "tools/call",
        "params": {
            "name": "execute_eagle_script",
            "arguments": args
        }
    }
    
    # Send the request
    req = urllib.request.Request(
        'http://localhost:8080/mcp',
        data=json.dumps(request).encode('utf-8'),
        headers={'Content-Type': 'application/json'}
    )
    
    try:
        response = urllib.request.urlopen(req)
        result = json.loads(response.read().decode('utf-8'))
        
        # Extract and display the result
        if 'result' in result and result['result']:
            content = json.loads(result['result']['content'][0]['text'])
            
            # Print the output
            print(content['result'])
            
            # Show execution details if available
            if 'isSuccess' in content:
                print(f"\nExecution {'succeeded' if content['isSuccess'] else 'failed'}")
            if 'executionId' in content:
                print(f"Execution ID: {content['executionId']}")
            if 'sessionId' in content and content['sessionId']:
                print(f"Session ID: {content['sessionId']}")
            
            return content
        else:
            print("Error: Unexpected response format")
            print(json.dumps(result, indent=2))
            return None
            
    except urllib.error.HTTPError as e:
        print(f"HTTP Error {e.code}: {e.reason}")
        print(e.read().decode('utf-8'))
        return None
    except Exception as e:
        print(f"Error: {e}")
        return None

def main():
    """Main entry point"""
    if len(sys.argv) < 2:
        print("Usage: python run_eagle_test.py <test_script> [options]")
        print("\nOptions:")
        print("  --session-id <id>     Use specific session ID")
        print("  --security <level>    Security level (Minimal, Standard, Elevated, Maximum)")
        print("  --format <format>     Output format (plain, json, xml, yaml, table, csv)")
        print("\nExamples:")
        print("  python run_eagle_test.py Phase1Complete.test.tcl")
        print("  python run_eagle_test.py SecurityPolicy.test.tcl --security Minimal")
        print("  python run_eagle_test.py SessionPersistenceVerify.test.tcl --session-id abc123")
        sys.exit(1)
    
    # Parse arguments
    script_name = sys.argv[1]
    session_id = None
    security_level = "Standard"
    output_format = "plain"
    
    i = 2
    while i < len(sys.argv):
        if sys.argv[i] == '--session-id' and i + 1 < len(sys.argv):
            session_id = sys.argv[i + 1]
            i += 2
        elif sys.argv[i] == '--security' and i + 1 < len(sys.argv):
            security_level = sys.argv[i + 1]
            i += 2
        elif sys.argv[i] == '--format' and i + 1 < len(sys.argv):
            output_format = sys.argv[i + 1]
            i += 2
        else:
            print(f"Unknown option: {sys.argv[i]}")
            sys.exit(1)
    
    # Find and run the test
    try:
        script_path = find_test_script(script_name)
        print(f"Running test: {os.path.basename(script_path)}")
        print("=" * 50)
        print()
        
        result = run_eagle_test(script_path, session_id, security_level, output_format)
        
        if result and not result.get('isSuccess', True):
            sys.exit(1)
            
    except FileNotFoundError as e:
        print(f"Error: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()