#!/bin/bash

# Rebuild and Test Script for DevOps MCP Server
# This script rebuilds the Docker container with the latest fixes and optionally runs tests

set -e

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}DevOps MCP Server - Rebuild and Test${NC}"
echo "======================================"
echo

# Parse arguments
RUN_TESTS=false
if [ "$1" == "--test" ]; then
    RUN_TESTS=true
fi

# Step 1: Stop any running containers
echo -e "${YELLOW}Stopping existing containers...${NC}"
docker stop devops-mcp 2>/dev/null || true
docker rm devops-mcp 2>/dev/null || true

# Step 2: Build the Docker image
echo -e "${YELLOW}Building Docker image...${NC}"
docker build -t devops-mcp .

if [ $? -ne 0 ]; then
    echo -e "${RED}Docker build failed!${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Docker image built successfully${NC}"
echo

# Step 3: Run the container
echo -e "${YELLOW}Starting the container...${NC}"
docker run -d \
    --name devops-mcp \
    -p 8080:8080 \
    -e AZURE_DEVOPS_ORG_URL="${AZURE_DEVOPS_ORG_URL:-https://dev.azure.com/example}" \
    -e AZURE_DEVOPS_PAT="${AZURE_DEVOPS_PAT:-dummy-pat-for-testing}" \
    devops-mcp

# Wait for container to be ready
echo -e "${YELLOW}Waiting for container to be ready...${NC}"
sleep 5

# Check if container is running
if ! docker ps | grep -q devops-mcp; then
    echo -e "${RED}Container failed to start!${NC}"
    echo "Container logs:"
    docker logs devops-mcp
    exit 1
fi

# Check if server is responding
for i in {1..10}; do
    if nc -z localhost 8080 2>/dev/null; then
        echo -e "${GREEN}✓ Server is running on port 8080${NC}"
        break
    fi
    if [ $i -eq 10 ]; then
        echo -e "${RED}Server failed to start on port 8080${NC}"
        echo "Container logs:"
        docker logs devops-mcp
        exit 1
    fi
    sleep 2
done

echo
echo -e "${GREEN}Container is running successfully!${NC}"
echo

# Step 4: Run tests if requested
if [ "$RUN_TESTS" == true ]; then
    echo -e "${YELLOW}Running test suite...${NC}"
    echo
    
    # Wait a bit more for full initialization
    sleep 5
    
    # Run the test suite
    cd tests/Eagle
    ./run_all_tests.sh
fi

echo
echo -e "${GREEN}Done!${NC}"
echo
echo "To view logs: docker logs -f devops-mcp"
echo "To run tests: cd tests/Eagle && ./run_all_tests.sh"
echo "To stop: docker stop devops-mcp"