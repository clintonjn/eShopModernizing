@echo off
echo Testing SQL Server container only...

echo Stopping any existing containers...
docker-compose -f docker-compose.sql-only.yml down -v

echo Starting SQL Server...
docker-compose -f docker-compose.sql-only.yml up -d

echo Waiting for SQL Server to be ready...
:wait_loop
docker-compose -f docker-compose.sql-only.yml ps sql-server | findstr "healthy" > nul
if %ERRORLEVEL% neq 0 (
    echo SQL Server is starting up, waiting...
    timeout /t 5 /nobreak > nul
    goto wait_loop
)

echo SQL Server is healthy!
echo Testing connection...
docker-compose -f docker-compose.sql-only.yml exec sql-server sqlcmd -S localhost -U sa -P Pass@word -Q "SELECT @@VERSION"

echo.
echo SQL Server is ready for use!
echo Connection string: Server=localhost,1433;Database=eShopCatalogDb;User Id=sa;Password=Pass@word;Encrypt=True;TrustServerCertificate=True;

pause