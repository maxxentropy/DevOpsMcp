import json
import urllib.request
import sys
import os

def run_security_test(security_level):
    """Run security test with specified security level"""
    
    # Try multiple paths to find the test script
    script_paths = [
        '/app/tests/Eagle/TestScripts/SecurityPolicy.test.tcl',  # In container
        os.path.join(os.path.dirname(__file__), '../TestScripts/SecurityPolicy.test.tcl'),  # Relative
        '/Users/sean/source/projects/DevOpsMcp/tests/Eagle/TestScripts/SecurityPolicy.test.tcl'  # Absolute
    ]
    
    script_content = None
    for path in script_paths:
        if os.path.exists(path):
            with open(path, 'r') as f:
                script_content = f.read()
            break
    
    if not script_content:
        print(f"Error: Could not find SecurityPolicy.test.tcl in any of the expected locations")
        sys.exit(1)
    
    # Create the request
    request = {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "tools/call",
        "params": {
            "name": "execute_eagle_script",
            "arguments": {
                "script": script_content,
                "securityLevel": security_level,
                "outputFormat": "plain"
            }
        }
    }
    
    print(f"Testing Security Level: {security_level}")
    print("=" * (24 + len(security_level)))
    
    # Send the request
    req = urllib.request.Request(
        'http://localhost:8080/mcp',
        data=json.dumps(request).encode('utf-8'),
        headers={'Content-Type': 'application/json'}
    )
    
    try:
        response = urllib.request.urlopen(req)
        result = json.loads(response.read().decode('utf-8'))
        
        # Extract and print the result
        if 'result' in result and result['result']:
            content = json.loads(result['result']['content'][0]['text'])
            print(content['result'])
        else:
            print(json.dumps(result, indent=2))
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python run_security_test.py <SecurityLevel>")
        print("SecurityLevel can be: Minimal, Standard, Elevated, or Maximum")
        sys.exit(1)
    
    run_security_test(sys.argv[1])