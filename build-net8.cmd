@echo off
echo [93m Building eShop .NET 8 Solution[0m

echo [93m Stopping any existing containers...[0m
docker-compose -f DevOps\docker-compose.net8.yml down -v

echo [93m Restoring NuGet packages...[0m
dotnet restore eShopNet8Solution\eShopNet8.sln
if %ERRORLEVEL% neq 0 (
    echo [91m Failed to restore NuGet packages[0m
    pause
    exit /b 1
)

echo [93m Building solution...[0m
dotnet build eShopNet8Solution\eShopNet8.sln --configuration Release --no-restore
if %ERRORLEVEL% neq 0 (
    echo [91m Failed to build solution[0m
    pause
    exit /b 1
)

echo [93m Starting services with Docker Compose...[0m
docker-compose -f DevOps\docker-compose.net8.yml up --build -d

echo [93m Waiting for SQL Server to be healthy...[0m
:wait_for_sql
docker-compose -f DevOps\docker-compose.net8.yml ps sql-server | findstr "healthy" > nul
if %ERRORLEVEL% neq 0 (
    echo [93m SQL Server is starting up, waiting...[0m
    timeout /t 5 /nobreak > nul
    goto wait_for_sql
)

echo [93m Waiting for services to start...[0m
timeout /t 15 /nobreak > nul

echo [93m Checking service status...[0m
docker-compose -f DevOps\docker-compose.net8.yml ps

echo [92m Build completed successfully![0m
echo [92m MVC Application: http://localhost:5115[0m
echo [92m API Service: http://localhost:5116[0m
echo [92m API Documentation: http://localhost:5116/swagger[0m
echo [92m SQL Server: localhost:1433[0m
echo.
echo [93m To stop services: docker-compose -f DevOps\docker-compose.net8.yml down[0m
echo [93m To view logs: docker-compose -f DevOps\docker-compose.net8.yml logs -f[0m
echo [93m To troubleshoot SQL: DevOps\scripts\check-sql-health.cmd[0m