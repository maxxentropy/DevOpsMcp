import json
import urllib.request
import threading
import time
from concurrent.futures import ThreadPoolExecutor, as_completed

def execute_script(script_id, delay=0):
    """Execute an Eagle script with a given ID"""
    
    # Create a unique script for each execution
    script_content = f"""
# Concurrent test script {script_id}
set scriptId {script_id}
set startTime [clock milliseconds]

# Simulate some work
set sum 0
for {{set i 1}} {{$i <= 500}} {{incr i}} {{
    set sum [expr {{$sum + $i}}]
}}

# Add a small delay if requested
if {{{delay} > 0}} {{
    after {delay}
}}

# Store result in session
set sessionKey "concurrent_test_$scriptId"
mcp::session set $sessionKey $sum

# Calculate execution time
set endTime [clock milliseconds]
set duration [expr {{$endTime - $startTime}}]

puts "Script $scriptId completed in ${{duration}}ms, sum=$sum"
"""
    
    request = {
        "jsonrpc": "2.0",
        "id": script_id,
        "method": "tools/call",
        "params": {
            "name": "execute_eagle_script",
            "arguments": {
                "script": script_content,
                "outputFormat": "plain"
            }
        }
    }
    
    try:
        req = urllib.request.Request(
            'http://localhost:8080/mcp',
            data=json.dumps(request).encode('utf-8'),
            headers={'Content-Type': 'application/json'}
        )
        
        start_time = time.time()
        response = urllib.request.urlopen(req)
        end_time = time.time()
        
        result = json.loads(response.read().decode('utf-8'))
        
        if 'result' in result and result['result']:
            content = json.loads(result['result']['content'][0]['text'])
            return {
                'script_id': script_id,
                'success': True,
                'output': content['result'],
                'response_time': end_time - start_time
            }
        else:
            return {
                'script_id': script_id,
                'success': False,
                'error': json.dumps(result),
                'response_time': end_time - start_time
            }
    except Exception as e:
        return {
            'script_id': script_id,
            'success': False,
            'error': str(e),
            'response_time': -1
        }

def main():
    print("Testing Interpreter Pool with Concurrent Requests")
    print("================================================")
    print()
    
    # Test parameters
    num_concurrent = 10  # Number of concurrent requests
    num_waves = 3       # Number of waves to test
    
    total_requests = 0
    total_success = 0
    total_failed = 0
    
    for wave in range(num_waves):
        print(f"\nWave {wave + 1} - Sending {num_concurrent} concurrent requests...")
        
        with ThreadPoolExecutor(max_workers=num_concurrent) as executor:
            # Submit all tasks
            futures = []
            for i in range(num_concurrent):
                script_id = wave * num_concurrent + i
                # Add small delays to some scripts to vary execution time
                delay = (i % 3) * 100  # 0ms, 100ms, or 200ms
                future = executor.submit(execute_script, script_id, delay)
                futures.append(future)
            
            # Collect results
            wave_success = 0
            wave_failed = 0
            response_times = []
            
            for future in as_completed(futures):
                result = future.result()
                total_requests += 1
                
                if result['success']:
                    wave_success += 1
                    total_success += 1
                    response_times.append(result['response_time'])
                    print(f"  ✓ Script {result['script_id']}: {result['output'].strip()}")
                else:
                    wave_failed += 1
                    total_failed += 1
                    print(f"  ✗ Script {result['script_id']}: {result['error']}")
            
            if response_times:
                avg_response_time = sum(response_times) / len(response_times)
                print(f"\nWave {wave + 1} Summary:")
                print(f"  Success: {wave_success}/{num_concurrent}")
                print(f"  Failed: {wave_failed}/{num_concurrent}")
                print(f"  Average response time: {avg_response_time:.3f}s")
        
        # Small delay between waves
        if wave < num_waves - 1:
            time.sleep(1)
    
    print("\n================================================")
    print("Overall Test Summary:")
    print(f"  Total requests: {total_requests}")
    print(f"  Total success: {total_success}")
    print(f"  Total failed: {total_failed}")
    print(f"  Success rate: {(total_success/total_requests*100):.1f}%")
    
    if total_failed == 0:
        print("\n✅ All concurrent requests completed successfully!")
        print("The interpreter pool is handling concurrent load properly.")
    else:
        print("\n⚠️  Some requests failed - check pool configuration")

if __name__ == "__main__":
    main()