#!/bin/bash

echo "🔧 Stopping existing containers..."
docker-compose -f docker-compose.simple.yml down

echo "🧹 Cleaning up old images..."
docker rmi devops-mcp:latest 2>/dev/null || true

echo "🏗️  Building fresh container with latest changes..."
docker-compose -f docker-compose.simple.yml build --no-cache

echo "✅ Container rebuilt successfully!"
echo ""
echo "To start the container, run:"
echo "  docker-compose -f docker-compose.simple.yml up"
echo ""
echo "Or to run in detached mode:"
echo "  docker-compose -f docker-compose.simple.yml up -d"