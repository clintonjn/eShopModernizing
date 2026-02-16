# eShop DevOps Configuration

This folder contains all the DevOps configurations and scripts for the modernized eShop application.

## Quick Start

### Local Development
```cmd
# From project root
build-net8.cmd
```

### AWS Cloud Deployment
```cmd
cd DevOps
./scripts/deploy-to-aws.sh eShop us-east-1 your-ecr-repo latest
```

## Folder Structure

```
DevOps/
├── cloudformation/           # AWS Infrastructure as Code
│   ├── vpc-infrastructure.yaml    # VPC, subnets, security groups
│   ├── rds-database.yaml         # RDS SQL Server instance
│   ├── ecs-cluster.yaml          # ECS Fargate cluster
│   └── ecs-services.yaml         # ECS services and load balancer
├── scripts/                  # Deployment and utility scripts
│   ├── deploy-to-aws.sh          # AWS deployment automation
│   ├── build-and-run-local.sh    # Local development script
│   ├── restart-local.cmd         # Windows restart script
│   ├── check-sql-health.cmd      # SQL Server diagnostics
│   └── test-sql-only.cmd         # SQL Server isolated test
├── docker-compose.net8.yml   # Local development containers
├── docker-compose.sql-only.yml   # SQL Server only for testing
├── README.md                 # This file
└── TROUBLESHOOTING.md        # Common issues and solutions
```

## Services

### Local Development (Docker Compose)

| Service | Port | Description |
|---------|------|-------------|
| **eshop-mvc-net8** | 5115 | ASP.NET Core MVC Web Application |
| **eshop-api-net8** | 5116 | REST API with Swagger documentation |
| **sql-server** | 1433 | SQL Server 2022 Express |

### AWS Cloud Services

| Service | Purpose |
|---------|---------|
| **VPC** | Isolated network environment |
| **RDS** | Managed SQL Server database |
| **ECS Fargate** | Serverless container orchestration |
| **Application Load Balancer** | Traffic distribution and SSL termination |
| **Parameter Store** | Secure configuration management |

## Local Development

### Prerequisites
- Docker Desktop for Windows
- .NET 8 SDK
- Windows 10/11 or Windows Server 2019+

### Starting Services
```cmd
# Quick start (recommended)
build-net8.cmd

# Manual start
cd DevOps
docker-compose -f docker-compose.net8.yml up --build -d
```

### Accessing Applications
- **Web Application**: http://localhost:5115
- **API Service**: http://localhost:5116
- **API Documentation**: http://localhost:5116/swagger
- **SQL Server**: localhost:1433 (sa/Pass@word)

### Stopping Services
```cmd
cd DevOps
docker-compose -f docker-compose.net8.yml down
```

## AWS Cloud Deployment

### Prerequisites
- AWS CLI configured with appropriate permissions
- Docker installed
- ECR repository created

### Infrastructure Deployment
```bash
# Deploy VPC and networking
aws cloudformation deploy \
  --template-file cloudformation/vpc-infrastructure.yaml \
  --stack-name eshop-vpc \
  --parameter-overrides ProjectName=eShop

# Deploy RDS database
aws cloudformation deploy \
  --template-file cloudformation/rds-database.yaml \
  --stack-name eshop-rds \
  --parameter-overrides ProjectName=eShop

# Deploy ECS cluster
aws cloudformation deploy \
  --template-file cloudformation/ecs-cluster.yaml \
  --stack-name eshop-ecs \
  --parameter-overrides ProjectName=eShop \
  --capabilities CAPABILITY_IAM

# Deploy services
aws cloudformation deploy \
  --template-file cloudformation/ecs-services.yaml \
  --stack-name eshop-services \
  --parameter-overrides ProjectName=eShop ImageTag=latest \
  --capabilities CAPABILITY_IAM
```

### Automated Deployment
```bash
# Single command deployment
./scripts/deploy-to-aws.sh eShop us-east-1 your-ecr-repo latest
```

## Configuration

### Environment Variables

#### Local Development
- `ASPNETCORE_ENVIRONMENT=Development`
- `ConnectionStrings__DefaultConnection` - SQL Server connection string

#### AWS Cloud
- `AWS__Region` - AWS region for Parameter Store
- `AWS__ParameterStore__ConnectionString` - Parameter Store path for connection string

### Database Configuration
- **Local**: SQL Server 2022 Express in Docker container
- **AWS**: RDS SQL Server with automated backups and encryption

### Logging
- **Local**: Console and file logging to `logs/` directory
- **AWS**: CloudWatch Logs integration

## Monitoring and Health Checks

### Health Endpoints
- **MVC**: http://localhost:5115/health
- **API**: http://localhost:5116/health

### Docker Health Checks
All services include health checks for proper startup sequencing.

### AWS Monitoring
- CloudWatch metrics and alarms
- Application Load Balancer health checks
- ECS service health monitoring

## Troubleshooting

For common issues and solutions, see [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

### Quick Diagnostics
```cmd
# Check service status
docker-compose -f docker-compose.net8.yml ps

# View logs
docker-compose -f docker-compose.net8.yml logs -f

# Test SQL Server
scripts\check-sql-health.cmd

# Complete reset
scripts\restart-local.cmd
```

## Security

### Local Development
- SQL Server uses default development credentials
- HTTPS redirection enabled
- CORS configured for development

### AWS Production
- RDS encryption at rest and in transit
- VPC with private subnets for database
- Security groups with minimal required access
- Parameter Store for secure credential management
- Application Load Balancer with SSL/TLS termination

## Performance

### Local Development
- Docker containers with resource limits
- SQL Server Express with appropriate memory allocation
- Health checks for proper startup sequencing

### AWS Production
- Auto-scaling ECS services based on CPU/memory utilization
- RDS with performance insights
- Application Load Balancer with connection draining
- CloudWatch monitoring and alerting

## Cost Optimization

### AWS Resources
- ECS Fargate for serverless container management
- RDS with appropriate instance sizing
- Application Load Balancer with efficient routing
- Parameter Store for configuration management (no additional cost)

### Estimated Monthly Costs (us-east-1)
- **ECS Fargate**: ~$30-50 (2 services, minimal traffic)
- **RDS SQL Server Express**: ~$15-25 (db.t3.micro)
- **Application Load Balancer**: ~$20-25
- **Data Transfer**: ~$5-10
- **Total**: ~$70-110/month

## Support

For issues or questions:
1. Check [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
2. Review service logs
3. Verify prerequisites and configuration
4. Test with isolated components (e.g., SQL Server only)