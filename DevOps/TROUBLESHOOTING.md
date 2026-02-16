# eShop .NET 8 Troubleshooting Guide

## Common Issues and Solutions

### 1. SQL Server Container Unhealthy

**Symptoms:**
- `container devops-sql-server-1 is unhealthy`
- Services fail to start with dependency errors

**Solutions:**

#### Option A: Use the SQL-only test
```cmd
cd DevOps
scripts\test-sql-only.cmd
```

#### Option B: Check SQL Server logs
```cmd
cd DevOps
docker-compose -f docker-compose.net8.yml logs sql-server
```

#### Option C: Manual SQL Server test
```cmd
# Start only SQL Server
docker-compose -f docker-compose.net8.yml up sql-server -d

# Wait a few minutes, then test connection
docker-compose -f docker-compose.net8.yml exec sql-server sqlcmd -S localhost -U sa -P Pass@word -Q "SELECT @@VERSION"
```

### 2. NuGet Package Restore Errors

**Symptoms:**
- `Unable to find package AWS.Extensions.NETCore.Setup`
- Build failures during Docker build

**Solutions:**

#### Clear NuGet cache
```cmd
dotnet nuget locals all --clear
dotnet restore eShopNet8Solution\eShopNet8.sln --force
```

#### Check package references
Ensure all projects have correct package references in their `.csproj` files.

### 3. Port Conflicts

**Symptoms:**
- `Port already in use` errors
- Services fail to bind to ports

**Solutions:**

#### Check what's using the ports
```cmd
netstat -ano | findstr :5115
netstat -ano | findstr :5116
netstat -ano | findstr :1433
```

#### Stop conflicting services
```cmd
# Stop all Docker containers
docker stop $(docker ps -aq)

# Or stop specific services
docker-compose -f DevOps\docker-compose.net8.yml down
```

### 4. Database Connection Issues

**Symptoms:**
- Connection timeout errors
- Database initialization failures

**Solutions:**

#### Verify SQL Server is running
```cmd
docker-compose -f DevOps\docker-compose.net8.yml ps sql-server
```

#### Test connection manually
```cmd
# From host machine
sqlcmd -S localhost,1433 -U sa -P Pass@word -Q "SELECT @@VERSION"

# From within container
docker-compose -f DevOps\docker-compose.net8.yml exec sql-server sqlcmd -S localhost -U sa -P Pass@word -Q "SELECT @@VERSION"
```

### 5. Application Startup Issues

**Symptoms:**
- Applications start but return errors
- Health checks fail

**Solutions:**

#### Check application logs
```cmd
# View all logs
docker-compose -f DevOps\docker-compose.net8.yml logs -f

# View specific service logs
docker-compose -f DevOps\docker-compose.net8.yml logs -f eshop-mvc-net8
docker-compose -f DevOps\docker-compose.net8.yml logs -f eshop-api-net8
```

#### Test health endpoints
```cmd
# Test MVC health
curl http://localhost:5115/health

# Test API health
curl http://localhost:5116/health
```

## Quick Recovery Steps

### Complete Reset
```cmd
# Stop everything
docker-compose -f DevOps\docker-compose.net8.yml down -v

# Clean Docker system
docker system prune -f

# Rebuild and start
build-net8.cmd
```

### Restart Services Only
```cmd
cd DevOps
scripts\restart-local.cmd
```

## Useful Commands

### Docker Management
```cmd
# View running containers
docker ps

# View all containers (including stopped)
docker ps -a

# View container logs
docker logs <container_name>

# Execute command in container
docker exec -it <container_name> bash

# Remove all stopped containers
docker container prune

# Remove all unused images
docker image prune -a
```

### .NET Development
```cmd
# Clean and rebuild solution
dotnet clean eShopNet8Solution\eShopNet8.sln
dotnet build eShopNet8Solution\eShopNet8.sln

# Run specific project
dotnet run --project eShopNet8Solution\src\eShopNet8.MVC
dotnet run --project eShopNet8Solution\src\eShopNet8.API
```

## Getting Help

If you continue to experience issues:

1. Check the logs: `docker-compose -f DevOps\docker-compose.net8.yml logs -f`
2. Verify system requirements: Docker Desktop, .NET 8 SDK
3. Ensure no other services are using ports 1433, 5115, 5116
4. Try the complete reset procedure above

## System Requirements

- Windows 10/11 or Windows Server 2019+
- Docker Desktop for Windows
- .NET 8 SDK
- At least 4GB RAM available for containers
- Ports 1433, 5115, 5116 available