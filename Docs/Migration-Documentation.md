# eShop Modernization Migration Documentation

## Executive Summary

This document provides comprehensive specifications for the migration of legacy .NET Framework applications to modernized containerized versions using Windows Containers and Azure cloud services. The migration follows a "Lift and Shift" approach, maintaining existing application architecture while enabling cloud-native deployment patterns.

## Table of Contents

1. [Migration Overview](#migration-overview)
2. [Framework Specifications](#framework-specifications)
3. [Database Migration](#database-migration)
4. [Application Architecture Changes](#application-architecture-changes)
5. [Containerization Strategy](#containerization-strategy)
6. [Configuration Management](#configuration-management)
7. [Deployment Options](#deployment-options)
8. [Migration Checklist](#migration-checklist)

## Migration Overview

### Project Scope
The eShop modernization project demonstrates migration of three application types:
- **ASP.NET MVC Application** - Product catalog management web application
- **ASP.NET WebForms Application** - Alternative web interface with same functionality
- **N-Tier Application** - WCF service backend with WinForms desktop client

### Migration Strategy
- **Approach**: Lift and Shift with containerization
- **Code Changes**: Minimal - primarily configuration and dependency injection enhancements
- **Database**: Schema preserved, connection management modernized
- **Deployment**: Multi-target (local Docker, Azure ACI, AKS, Web Apps, VMs)

## Framework Specifications

### Legacy Framework Versions

| Application Type | Framework Version | Target Framework | Key Dependencies |
|------------------|-------------------|------------------|------------------|
| eShopLegacyMVC | .NET Framework 4.7.2 | Traditional ASP.NET MVC | Entity Framework 6.1.3, Autofac |
| eShopLegacyWebForms | .NET Framework 4.7.2 | Traditional ASP.NET WebForms | Entity Framework 6.1.3, Autofac |
| eShopLegacyWCF | .NET Framework 4.6.1 | WCF Service | Entity Framework 6.1.3 |
| eShopLegacyWinForms | .NET Framework 4.7.1 | Windows Forms | Entity Framework 6.0.0.0 |

### Modernized Framework Versions

| Application Type | Framework Version | Target Framework | Key Dependencies | Container Base Image |
|------------------|-------------------|------------------|------------------|---------------------|
| eShopModernizedMVC | .NET Framework 4.7.2 | ASP.NET MVC (Containerized) | Entity Framework 6.3.0, Autofac | mcr.microsoft.com/dotnet/framework/aspnet:4.7.2 |
| eShopModernizedWebForms | .NET Framework 4.7.2 | ASP.NET WebForms (Containerized) | Entity Framework 6.3.0, Autofac | mcr.microsoft.com/dotnet/framework/aspnet:4.7.2 |
| eShopModernizedWCF | .NET Framework 4.7.2 | WCF Service (Containerized) | Entity Framework 6.1.3 | mcr.microsoft.com/dotnet/framework/wcf:4.7.2 |
| eShopModernizedWinForms | .NET 6.0 | Windows Forms + WPF | Entity Framework 6.4.4, WCF Client | Native Windows Application |

### Additional Modernized Version

| Application Type | Framework Version | Target Framework | Key Dependencies | Container Base Image |
|------------------|-------------------|------------------|------------------|---------------------|
| eShopPorted | .NET 8.0 | ASP.NET Core MVC | Entity Framework Core, Built-in DI | mcr.microsoft.com/dotnet/aspnet:8.0 |

## Database Migration

### Legacy Database Configuration

```xml
<!-- Legacy Web.config -->
<connectionStrings>
  <add name="CatalogDBContext" 
       connectionString="Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Microsoft.eShopOnContainers.Services.CatalogDb;Integrated Security=True;MultipleActiveResultSets=True" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

### Modernized Database Configuration

#### Connection String Sources (Priority Order)
1. **Environment Variables** (Highest Priority)
2. **Azure Key Vault** (Production)
3. **User Secrets** (Development)
4. **Web.config** (Fallback)

#### Database Connection Patterns

```csharp
// Legacy Pattern
public CatalogDBContext() : base("name=CatalogDBContext") { }

// Modernized Pattern with Dependency Injection
public CatalogDBContext(ISqlConnectionFactory provider) 
    : base(provider.CreateConnection(), true) { }
```

#### Connection Factory Implementations

```csharp
// App Settings Connection Factory
public class AppSettingsSqlConnectionFactory : ISqlConnectionFactory
{
    public DbConnection CreateConnection()
    {
        var connectionString = ConfigurationManager.ConnectionStrings["CatalogDBContext"].ConnectionString;
        return new SqlConnection(connectionString);
    }
}

// Managed Identity Connection Factory (Azure)
public class ManagedIdentitySqlConnectionFactory : ISqlConnectionFactory
{
    public DbConnection CreateConnection()
    {
        // Uses Azure Managed Identity for authentication
        var connectionString = Environment.GetEnvironmentVariable("ConnectionString");
        var connection = new SqlConnection(connectionString);
        connection.AccessToken = GetAzureAccessToken();
        return connection;
    }
}
```

### Database Schema

#### Core Tables

| Table Name | Purpose | Key Columns |
|------------|---------|-------------|
| `Catalog` | Product catalog items | Id (PK), Name, Description, Price, PictureFileName, CatalogBrandId (FK), CatalogTypeId (FK) |
| `CatalogBrand` | Product brands/manufacturers | Id (PK), Brand |
| `CatalogType` | Product categories | Id (PK), Type |
| `CatalogItemsStock` | Stock tracking (N-Tier only) | Id (PK), CatalogItemId (FK), AvailableStock, Date |
| `DiscountItem` | Discount information (N-Tier only) | Id (PK), CatalogItemId (FK), Discount, StartDate, EndDate |

#### Entity Framework Configuration

```csharp
// Catalog Item Configuration
void ConfigureCatalogItem(EntityTypeConfiguration<CatalogItem> builder)
{
    builder.ToTable("Catalog");
    builder.HasKey(ci => ci.Id);
    builder.Property(ci => ci.Id)
        .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None)
        .IsRequired();
    builder.Property(ci => ci.Name)
        .IsRequired()
        .HasMaxLength(50);
    builder.Property(ci => ci.Price)
        .IsRequired();
    builder.HasRequired<CatalogBrand>(ci => ci.CatalogBrand)
        .WithMany()
        .HasForeignKey(ci => ci.CatalogBrandId);
    builder.HasRequired<CatalogType>(ci => ci.CatalogType)
        .WithMany()
        .HasForeignKey(ci => ci.CatalogTypeId);
}
```

### Database Initialization Strategy

```csharp
public class CatalogDBInitializer : CreateDatabaseIfNotExists<CatalogDBContext>
{
    protected override void Seed(CatalogDBContext context)
    {
        // Seed catalog types from CSV
        GetCatalogTypesFromFile().ForEach(ct => context.CatalogTypes.Add(ct));
        
        // Seed catalog brands from CSV
        GetCatalogBrandsFromFile().ForEach(cb => context.CatalogBrands.Add(cb));
        
        // Seed catalog items from CSV
        GetCatalogItemsFromFile(context).ForEach(ci => context.CatalogItems.Add(ci));
        
        context.SaveChanges();
    }
}
```

## Application Architecture Changes

### Dependency Injection Evolution

#### Legacy Registration (Basic)
```csharp
protected IContainer RegisterContainer()
{
    var builder = new ContainerBuilder();
    builder.RegisterControllers(typeof(MvcApplication).Assembly);
    builder.RegisterType<CatalogService>().As<ICatalogService>();
    builder.RegisterType<CatalogDBContext>().InstancePerLifetimeScope();
    return builder.Build();
}
```

#### Modernized Registration (Enhanced)
```csharp
public class ApplicationModule : Module
{
    private bool useMockData;
    private bool useAzureStorage;
    private bool useManagedIdentity;

    protected override void Load(ContainerBuilder builder)
    {
        // Conditional service registration based on configuration
        if (this.useMockData)
        {
            builder.RegisterType<CatalogServiceMock>()
                .As<ICatalogService>()
                .SingleInstance();
        }
        else
        {
            builder.RegisterType<CatalogService>()
                .As<ICatalogService>()
                .InstancePerLifetimeScope();
        }

        // Storage service selection
        if (this.useAzureStorage)
        {
            builder.RegisterType<ImageAzureStorage>()
                .As<IImageService>()
                .InstancePerLifetimeScope();
        }
        else
        {
            builder.RegisterType<ImageMockStorage>()
                .As<IImageService>()
                .InstancePerLifetimeScope();
        }

        // Connection factory selection
        if (this.useManagedIdentity)
        {
            builder.RegisterType<ManagedIdentitySqlConnectionFactory>()
                .As<ISqlConnectionFactory>()
                .SingleInstance();
        }
        else
        {
            builder.RegisterType<AppSettingsSqlConnectionFactory>()
                .As<ISqlConnectionFactory>()
                .SingleInstance();
        }
    }
}
```

### Configuration Management

#### Configuration Builders (Modernized)
```xml
<configuration>
  <configBuilders>
    <builders>
      <add name="Secrets" userSecretsId="aspnet-eShopModernizedMVC" type="Microsoft.Configuration.ConfigurationBuilders.UserSecretsConfigurationBuilder" />
      <add name="Environment" type="Microsoft.Configuration.ConfigurationBuilders.EnvironmentConfigurationBuilder" />
      <add name="AzureKeyVault" vaultName="[vault-name]" type="Microsoft.Configuration.ConfigurationBuilders.AzureKeyVaultConfigurationBuilder" />
    </builders>
  </configBuilders>
  
  <connectionStrings configBuilders="AzureKeyVault,Environment,Secrets">
    <add name="CatalogDBContext" connectionString="[fallback-connection]" />
  </connectionStrings>
  
  <appSettings configBuilders="AzureKeyVault,Environment,Secrets">
    <add key="UseMockData" value="false" />
    <add key="UseAzureStorage" value="false" />
    <add key="UseManagedIdentity" value="false" />
  </appSettings>
</configuration>
```

## Containerization Strategy

### Docker Configuration

#### Multi-Service Docker Compose
```yaml
version: '3.4'

services:
  eshop.modernized.mvc:
    image: eshop/modernizedmvc:${TAG:-latest}
    build:
      context: ./deploy/mvc
      dockerfile: Dockerfile
    ports:
      - "5115:80"
    environment:
      - CatalogDBContext=Server=sql.data;Database=Microsoft.eShopOnContainers.Services.CatalogDb;User Id=sa;Password=Pass@word
      - UseMockData=False
      - UseCustomizationData=False
      - UseAzureStorage=False
    depends_on:
      - sql.data

  eshop.modernized.webforms:
    image: eshop/modernizedwebforms:${TAG:-latest}
    build:
      context: ./deploy/webforms
      dockerfile: Dockerfile
    ports:
      - "5114:80"
    environment:
      - CatalogDBContext=Server=sql.data;Database=Microsoft.eShopOnContainers.Services.CatalogDb;User Id=sa;Password=Pass@word
      - UseMockData=False
    depends_on:
      - sql.data

  eshopwcfservice:
    image: eshop/wcfservice:${TAG:-latest}
    build:
      context: ./deploy/wcf
      dockerfile: Dockerfile
    ports:
      - "5113:80"
    environment:
      - CatalogDBContext=Server=sql.data;Database=eShopDatabase;User Id=sa;Password=Pass@word
    depends_on:
      - sql.data

  sql.data:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - SA_PASSWORD=Pass@word
      - ACCEPT_EULA=Y
    ports:
      - "5433:1433"
    healthcheck:
      test: ["CMD-SHELL", "sqlcmd -S localhost -U sa -P Pass@word -Q 'SELECT 1'"]
      interval: 30s
      timeout: 10s
      retries: 5
```

#### Application Dockerfiles

**ASP.NET MVC/WebForms Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.7.2
ARG source
WORKDIR /inetpub/wwwroot
EXPOSE 80
COPY ${source:-obj/Docker/publish} .
```

**WCF Service Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/framework/wcf:4.7.2
EXPOSE 80
ARG source
WORKDIR /inetpub/wwwroot
COPY ${source:-obj/Docker/publish} .
```

### Build Process

#### Automated Build Script (build.cmd)
```batch
@echo Building MVC project...
nuget restore eShopModernizedMVCSolution\eShopModernizedMVC.sln
msbuild eShopModernizedMVCSolution\src\eShopModernizedMVC\eShopModernizedMVC.csproj /p:PublishProfile=FolderProfile.pubxml /p:DeployOnBuild=true /p:docker_publish_root=..\..\..\deploy\mvc\

@echo Building Webforms project...
nuget restore eShopModernizedWebFormsSolution\eShopModernizedWebForms.sln
msbuild eShopModernizedWebFormsSolution\src\eShopModernizedWebForms\eShopModernizedWebForms.csproj /p:PublishProfile=FolderProfile.pubxml /p:DeployOnBuild=true /p:docker_publish_root=..\..\..\deploy\webforms\

@echo Building WCF project...
nuget restore eShopModernizedNTier\eShopModernizedNTier.sln
msbuild eShopModernizedNTier\src\eShopWCFService\eShopWCFService.csproj /p:PublishProfile=FolderProfile.pubxml /p:DeployOnBuild=true /p:docker_publish_root=..\..\..\deploy\wcf\

@echo Building docker images...
docker-compose -f docker-compose.yml -f docker-compose.override.yml build
```

## Configuration Management

### Environment Variables

| Variable Name | Purpose | Default Value | Example |
|---------------|---------|---------------|---------|
| `CatalogDBContext` | Database connection string | Local connection | `Server=sql.data;Database=CatalogDb;User Id=sa;Password=Pass@word` |
| `UseMockData` | Enable mock data mode | `False` | `True` for testing |
| `UseCustomizationData` | Use custom seed data | `False` | `True` for demo data |
| `UseAzureStorage` | Enable Azure Blob Storage | `False` | `True` for production |
| `StorageConnectionString` | Azure Storage connection | Empty | Azure storage account connection |
| `AppInsightsInstrumentationKey` | Application Insights key | Empty | Azure Application Insights key |
| `UseAzureActiveDirectory` | Enable Azure AD auth | `False` | `True` for enterprise auth |
| `AzureActiveDirectoryClientId` | Azure AD client ID | Empty | Azure AD application ID |
| `AzureActiveDirectoryTenant` | Azure AD tenant | Empty | Azure AD tenant ID |

### Configuration Hierarchy (Priority Order)
1. **Environment Variables** (Highest)
2. **Azure Key Vault**
3. **User Secrets** (Development)
4. **Web.config/App.config** (Lowest)

## Deployment Options

### 1. Local Development (Docker for Windows)
```bash
# Build and run locally
build.cmd
docker-compose up
```

**Access URLs:**
- MVC Application: http://localhost:5115
- WebForms Application: http://localhost:5114
- WCF Service: http://localhost:5113
- SQL Server: localhost:5433

### 2. Azure Container Instances (ACI)
```bash
# Deploy to ACI using Azure CLI
az container create --resource-group myResourceGroup \
  --name eshop-mvc \
  --image eshop/modernizedmvc:latest \
  --ports 80 \
  --environment-variables \
    CatalogDBContext="Server=myserver.database.windows.net;Database=CatalogDb;User Id=myuser;Password=mypassword" \
    UseMockData=False
```

### 3. Azure Kubernetes Service (AKS)
```yaml
# Kubernetes deployment manifest
apiVersion: apps/v1
kind: Deployment
metadata:
  name: eshop-mvc
spec:
  replicas: 3
  selector:
    matchLabels:
      app: eshop-mvc
  template:
    metadata:
      labels:
        app: eshop-mvc
    spec:
      containers:
      - name: eshop-mvc
        image: eshop/modernizedmvc:latest
        ports:
        - containerPort: 80
        env:
        - name: CatalogDBContext
          valueFrom:
            secretKeyRef:
              name: database-secret
              key: connection-string
```

### 4. Azure Web App for Containers
```bash
# Deploy to Azure Web App
az webapp create --resource-group myResourceGroup \
  --plan myAppServicePlan \
  --name myeShopApp \
  --deployment-container-image-name eshop/modernizedmvc:latest
```

### 5. Windows Server VM
```powershell
# Install Docker on Windows Server
Install-Module -Name DockerMsftProvider -Repository PSGallery -Force
Install-Package -Name docker -ProviderName DockerMsftProvider

# Deploy application
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

## Migration Checklist

### Pre-Migration Assessment
- [ ] Inventory existing applications and dependencies
- [ ] Identify database connections and configurations
- [ ] Document current deployment processes
- [ ] Assess Azure subscription and resource requirements
- [ ] Plan network and security configurations

### Code Migration Steps
- [ ] Update project files to latest .NET Framework version (4.7.2+)
- [ ] Implement ISqlConnectionFactory pattern for database connections
- [ ] Add configuration builders for external configuration sources
- [ ] Enhance dependency injection with conditional registrations
- [ ] Add support for mock data and Azure services
- [ ] Create Dockerfiles for each application type
- [ ] Configure docker-compose orchestration

### Database Migration Steps
- [ ] Backup existing databases
- [ ] Update Entity Framework to version 6.3.0+
- [ ] Test database initialization and seeding
- [ ] Configure connection strings for container environments
- [ ] Set up Azure SQL Database (if using cloud)
- [ ] Test database connectivity from containers

### Container Configuration
- [ ] Create Docker images for each application
- [ ] Configure environment variables and secrets
- [ ] Set up health checks and monitoring
- [ ] Test local container deployment
- [ ] Configure container registries (Azure Container Registry)

### Azure Deployment
- [ ] Set up Azure resource groups and networking
- [ ] Configure Azure Key Vault for secrets management
- [ ] Set up Application Insights for monitoring
- [ ] Configure Azure Active Directory (if required)
- [ ] Deploy to chosen Azure service (ACI, AKS, Web Apps)
- [ ] Configure load balancing and scaling
- [ ] Set up CI/CD pipelines

### Testing and Validation
- [ ] Functional testing of migrated applications
- [ ] Performance testing under load
- [ ] Security testing and vulnerability assessment
- [ ] Disaster recovery testing
- [ ] User acceptance testing
- [ ] Documentation and training

### Post-Migration
- [ ] Monitor application performance and health
- [ ] Set up alerting and notifications
- [ ] Plan for ongoing maintenance and updates
- [ ] Document lessons learned and best practices
- [ ] Train operations team on container management

## Conclusion

This migration documentation provides a comprehensive guide for modernizing legacy .NET Framework applications using Windows Containers and Azure cloud services. The "Lift and Shift" approach minimizes code changes while enabling cloud-native deployment patterns, improved scalability, and enhanced operational capabilities.

The migration maintains application functionality while adding support for:
- Container-based deployment
- Cloud-native configuration management
- Multiple deployment targets
- Enhanced monitoring and diagnostics
- Improved security through managed identities and Key Vault integration

For additional information and detailed implementation guides, refer to the project Wiki and Microsoft's official documentation on modernizing .NET applications with Azure and Windows Containers.