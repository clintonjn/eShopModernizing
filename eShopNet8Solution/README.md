# eShop .NET 8 Modernized Solution

This is the fully modernized version of the eShop application, migrated from .NET Framework to .NET 8 with cloud-native architecture and AWS deployment capabilities.

## Architecture Overview

The solution follows a clean architecture pattern with the following projects:

- **eShopNet8.Shared** - Common models and contracts
- **eShopNet8.Infrastructure** - Data access and external services
- **eShopNet8.MVC** - Web application with MVC pattern
- **eShopNet8.API** - RESTful API service

## Key Features

### .NET 8 Modernization
- ✅ Migrated from .NET Framework 4.7.2 to .NET 8
- ✅ Modern C# features with nullable reference types
- ✅ Minimal APIs and improved performance
- ✅ Native dependency injection
- ✅ Configuration providers and options pattern

### Cloud-Native Architecture
- ✅ Container-first design with Linux containers
- ✅ Health checks and monitoring
- ✅ Structured logging with Serilog
- ✅ AWS Parameter Store integration
- ✅ Horizontal scaling support

### Database Modernization
- ✅ Entity Framework Core 8.0
- ✅ Code-first migrations
- ✅ Connection resilience and retry policies
- ✅ AWS RDS SQL Server support

### DevOps & Deployment
- ✅ Docker containerization
- ✅ AWS CloudFormation infrastructure as code
- ✅ ECS Fargate deployment
- ✅ Application Load Balancer with health checks
- ✅ Auto-scaling policies

## Quick Start

### Local Development

1. **Prerequisites**
   - .NET 8 SDK
   - Docker Desktop
   - SQL Server (or use Docker Compose)

2. **Run with Docker Compose**
   ```bash
   cd DevOps
   chmod +x scripts/build-and-run-local.sh
   ./scripts/build-and-run-local.sh
   ```

3. **Access Applications**
   - MVC Web App: http://localhost:5115
   - API Service: http://localhost:5116
   - API Documentation: http://localhost:5116/swagger

### AWS Deployment

1. **Prerequisites**
   - AWS CLI configured
   - Docker installed
   - ECR repository created

2. **Deploy to AWS**
   ```bash
   cd DevOps
   chmod +x scripts/deploy-to-aws.sh
   ./scripts/deploy-to-aws.sh eShop us-east-1 your-ecr-repo latest
   ```

## Project Structure

```
eShopNet8Solution/
├── src/
│   ├── eShopNet8.Shared/          # Common models and contracts
│   │   └── Models/                # Domain models
│   ├── eShopNet8.Infrastructure/  # Data access layer
│   │   ├── Data/                  # Entity Framework context
│   │   └── Services/              # Business services
│   ├── eShopNet8.MVC/            # Web application
│   │   ├── Controllers/           # MVC controllers
│   │   ├── Views/                 # Razor views
│   │   └── wwwroot/              # Static files
│   └── eShopNet8.API/            # REST API
│       └── Controllers/           # API controllers
└── DevOps/                       # Deployment configurations
    ├── cloudformation/           # AWS infrastructure templates
    ├── scripts/                  # Deployment scripts
    └── docker-compose.net8.yml   # Local development
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Application environment | Development |
| `ConnectionStrings__DefaultConnection` | Database connection string | Local SQL Server |
| `AWS__Region` | AWS region for services | us-east-1 |
| `AWS__ParameterStore__ConnectionString` | Parameter Store path for connection string | /eShop/database/connectionstring |

### AWS Parameter Store

The application uses AWS Systems Manager Parameter Store for configuration:

- `/eShop/database/connectionstring` - Database connection string (SecureString)
- `/eShop/database/endpoint` - RDS endpoint
- `/eShop/database/password` - Database password (SecureString)

## API Endpoints

### Catalog API

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/catalog` | Get catalog items with filtering |
| GET | `/api/catalog/{id}` | Get specific catalog item |
| POST | `/api/catalog` | Create new catalog item |
| PUT | `/api/catalog/{id}` | Update catalog item |
| DELETE | `/api/catalog/{id}` | Delete catalog item |
| GET | `/api/catalog/brands` | Get all catalog brands |
| GET | `/api/catalog/types` | Get all catalog types |

### Health Checks

| Endpoint | Description |
|----------|-------------|
| `/health` | Application health status |

## Database Schema

### Tables

- **Catalog** - Product catalog items
- **CatalogBrand** - Product brands/manufacturers  
- **CatalogType** - Product categories/types

### Key Features

- Automatic migrations on startup
- Seed data for development
- Optimized indexes for performance
- Foreign key constraints with cascade rules

## Monitoring & Logging

### Structured Logging
- Serilog with console and file sinks
- JSON structured logging in production
- Correlation IDs for request tracing

### Health Checks
- Database connectivity checks
- Custom application health indicators
- Integration with AWS Application Load Balancer

### Metrics
- Built-in .NET metrics
- Custom business metrics
- CloudWatch integration (when deployed to AWS)

## Security

### Authentication & Authorization
- Ready for integration with AWS Cognito
- JWT token support
- Role-based access control

### Data Protection
- Connection string encryption in Parameter Store
- SQL injection protection with Entity Framework
- Input validation and sanitization

## Performance

### Optimizations
- Async/await throughout the application
- Entity Framework query optimization
- Response caching for static data
- Connection pooling and resilience

### Scaling
- Stateless application design
- Horizontal scaling with ECS Fargate
- Auto-scaling based on CPU utilization
- Load balancing across multiple instances

## Migration Notes

### From .NET Framework
- Replaced System.Web with ASP.NET Core
- Updated Entity Framework 6 to Entity Framework Core 8
- Migrated from Autofac to built-in DI container
- Updated configuration from Web.config to appsettings.json
- Replaced IIS hosting with Kestrel

### Breaking Changes
- Namespace changes for all projects
- Updated NuGet package references
- Modified connection string format
- Updated logging configuration

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.