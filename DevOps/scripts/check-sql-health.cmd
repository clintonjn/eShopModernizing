@echo off
echo Checking SQL Server container status...
docker-compose -f docker-compose.net8.yml ps

echo.
echo Checking SQL Server logs...
docker-compose -f docker-compose.net8.yml logs sql-server

echo.
echo Testing SQL Server connection...
docker-compose -f docker-compose.net8.yml exec sql-server sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q "SELECT @@VERSION"

pause