#!/bin/bash

# eShop .NET 8 Local Development Script
set -e

echo "Building and running eShop .NET 8 locally..."

# Navigate to DevOps directory
cd "$(dirname "$0")/.."

# Build and start services
echo "Starting services with Docker Compose..."
docker-compose -f docker-compose.net8.yml up --build -d

echo "Waiting for services to be healthy..."
sleep 30

# Check service health
echo "Checking service health..."

# Check MVC health
MVC_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5115/health || echo "000")
if [ "$MVC_HEALTH" = "200" ]; then
    echo "✅ MVC service is healthy"
else
    echo "❌ MVC service is not healthy (HTTP $MVC_HEALTH)"
fi

# Check API health
API_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5116/health || echo "000")
if [ "$API_HEALTH" = "200" ]; then
    echo "✅ API service is healthy"
else
    echo "❌ API service is not healthy (HTTP $API_HEALTH)"
fi

# Check SQL Server
SQL_HEALTH=$(docker-compose -f docker-compose.net8.yml exec -T sql-server /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Pass@word -Q "SELECT 1" > /dev/null 2>&1 && echo "200" || echo "000")
if [ "$SQL_HEALTH" = "200" ]; then
    echo "✅ SQL Server is healthy"
else
    echo "❌ SQL Server is not healthy"
fi

echo ""
echo "🚀 eShop .NET 8 is running locally!"
echo ""
echo "Services:"
echo "  📱 MVC Application: http://localhost:5115"
echo "  🔌 API Service: http://localhost:5116"
echo "  📚 API Documentation: http://localhost:5116/swagger"
echo "  🗄️  SQL Server: localhost:1433"
echo ""
echo "To stop services: docker-compose -f docker-compose.net8.yml down"
echo "To view logs: docker-compose -f docker-compose.net8.yml logs -f"