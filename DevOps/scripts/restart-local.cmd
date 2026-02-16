@echo off
echo Stopping and removing existing containers...
docker-compose -f docker-compose.net8.yml down -v

echo Cleaning up Docker system...
docker system prune -f

echo Building and starting services...
docker-compose -f docker-compose.net8.yml up --build

pause