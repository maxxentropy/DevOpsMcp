#!/bin/bash

echo "Running All Security Policy Tests"
echo "================================="
echo

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "1. Testing Minimal Security Level"
echo "---------------------------------"
python3 "$SCRIPT_DIR/run_security_test.py" Minimal
echo
echo

echo "2. Testing Standard Security Level"
echo "----------------------------------"
python3 "$SCRIPT_DIR/run_security_test.py" Standard
echo
echo

echo "3. Testing Elevated Security Level"
echo "----------------------------------"
python3 "$SCRIPT_DIR/run_security_test.py" Elevated
echo
echo

echo "4. Testing Maximum Security Level"
echo "---------------------------------"
python3 "$SCRIPT_DIR/run_security_test.py" Maximum
echo

echo "================================="
echo "Security Policy Tests Complete"