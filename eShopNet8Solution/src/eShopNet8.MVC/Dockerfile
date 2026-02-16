FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/eShopNet8.MVC/eShopNet8.MVC.csproj", "src/eShopNet8.MVC/"]
COPY ["src/eShopNet8.Infrastructure/eShopNet8.Infrastructure.csproj", "src/eShopNet8.Infrastructure/"]
COPY ["src/eShopNet8.Shared/eShopNet8.Shared.csproj", "src/eShopNet8.Shared/"]
RUN dotnet restore "src/eShopNet8.MVC/eShopNet8.MVC.csproj"
COPY . .
WORKDIR "/src/src/eShopNet8.MVC"
RUN dotnet build "eShopNet8.MVC.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "eShopNet8.MVC.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Add health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "eShopNet8.MVC.dll"]