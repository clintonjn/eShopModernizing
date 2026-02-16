#!/bin/bash

# eShop .NET 8 AWS Deployment Script
set -e

# Configuration
ENVIRONMENT_NAME=${1:-eShop}
AWS_REGION=${2:-us-east-1}
ECR_REPOSITORY_URI=${3:-"your-account-id.dkr.ecr.us-east-1.amazonaws.com/eshop"}
IMAGE_TAG=${4:-latest}

echo "Deploying eShop .NET 8 to AWS"
echo "Environment: $ENVIRONMENT_NAME"
echo "Region: $AWS_REGION"
echo "ECR Repository: $ECR_REPOSITORY_URI"
echo "Image Tag: $IMAGE_TAG"

# Function to check if stack exists
stack_exists() {
    aws cloudformation describe-stacks --stack-name $1 --region $AWS_REGION > /dev/null 2>&1
}

# Function to wait for stack completion
wait_for_stack() {
    echo "Waiting for stack $1 to complete..."
    aws cloudformation wait stack-create-complete --stack-name $1 --region $AWS_REGION 2>/dev/null || \
    aws cloudformation wait stack-update-complete --stack-name $1 --region $AWS_REGION
    echo "Stack $1 completed successfully"
}

# Step 1: Deploy VPC Infrastructure
echo "Step 1: Deploying VPC Infrastructure..."
VPC_STACK_NAME="${ENVIRONMENT_NAME}-vpc"

if stack_exists $VPC_STACK_NAME; then
    echo "Updating existing VPC stack..."
    aws cloudformation update-stack \
        --stack-name $VPC_STACK_NAME \
        --template-body file://cloudformation/vpc-infrastructure.yaml \
        --parameters ParameterKey=EnvironmentName,ParameterValue=$ENVIRONMENT_NAME \
        --region $AWS_REGION || echo "No changes to VPC stack"
else
    echo "Creating new VPC stack..."
    aws cloudformation create-stack \
        --stack-name $VPC_STACK_NAME \
        --template-body file://cloudformation/vpc-infrastructure.yaml \
        --parameters ParameterKey=EnvironmentName,ParameterValue=$ENVIRONMENT_NAME \
        --region $AWS_REGION
fi

wait_for_stack $VPC_STACK_NAME

# Step 2: Deploy RDS Database
echo "Step 2: Deploying RDS Database..."
RDS_STACK_NAME="${ENVIRONMENT_NAME}-rds"

# Generate random password if not provided
DB_PASSWORD=${DB_PASSWORD:-$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)}

if stack_exists $RDS_STACK_NAME; then
    echo "Updating existing RDS stack..."
    aws cloudformation update-stack \
        --stack-name $RDS_STACK_NAME \
        --template-body file://cloudformation/rds-database.yaml \
        --parameters \
            ParameterKey=EnvironmentName,ParameterValue=$ENVIRONMENT_NAME \
            ParameterKey=DatabasePassword,ParameterValue=$DB_PASSWORD \
        --region $AWS_REGION || echo "No changes to RDS stack"
else
    echo "Creating new RDS stack..."
    aws cloudformation create-stack \
        --stack-name $RDS_STACK_NAME \
        --template-body file://cloudformation/rds-database.yaml \
        --parameters \
            ParameterKey=EnvironmentName,ParameterValue=$ENVIRONMENT_NAME \
            ParameterKey=DatabasePassword,ParameterValue=$DB_PASSWORD \
        --capabilities CAPABILITY_IAM \
        --region $AWS_REGION
fi

wait_for_stack $RDS_STACK_NAME

# Step 3: Deploy ECS Cluster
echo "Step 3: Deploying ECS Cluster..."
ECS_STACK_NAME="${ENVIRONMENT_NAME}-ecs"

if stack_exists $ECS_STACK_NAME; then
    echo "Updating existing ECS stack..."
    aws cloudformation update-stack \
        --stack-name $ECS_STACK_NAME \
        --template-body file://cloudformation/ecs-cluster.yaml \
        --parameters \
            ParameterKey=EnvironmentName,ParameterValue=$ENVIRONMENT_NAME \
            ParameterKey=ECRRepository,ParameterValue=$ECR_REPOSITORY_URI \
        --capabilities CAPABILITY_IAM \
        --region $AWS_REGION || echo "No changes to ECS stack"
else
    echo "Creating new ECS stack..."
    aws cloudformation create-stack \
        --stack-name $ECS_STACK_NAME \
        --template-body file://cloudformation/ecs-cluster.yaml \
        --parameters \
            ParameterKey=EnvironmentName,ParameterValue=$ENVIRONMENT_NAME \
            ParameterKey=ECRRepository,ParameterValue=$ECR_REPOSITORY_URI \
        --capabilities CAPABILITY_IAM \
        --region $AWS_REGION
fi

wait_for_stack $ECS_STACK_NAME

# Step 4: Build and Push Docker Images
echo "Step 4: Building and pushing Docker images..."

# Login to ECR
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $ECR_REPOSITORY_URI

# Build and push MVC image
echo "Building MVC image..."
cd ../eShopNet8Solution
docker build -f src/eShopNet8.MVC/Dockerfile -t $ECR_REPOSITORY_URI/mvc:$IMAGE_TAG .
docker push $ECR_REPOSITORY_URI/mvc:$IMAGE_TAG

# Build and push API image
echo "Building API image..."
docker build -f src/eShopNet8.API/Dockerfile -t $ECR_REPOSITORY_URI/api:$IMAGE_TAG .
docker push $ECR_REPOSITORY_URI/api:$IMAGE_TAG

cd ../DevOps

# Step 5: Deploy ECS Services
echo "Step 5: Deploying ECS Services..."
SERVICES_STACK_NAME="${ENVIRONMENT_NAME}-services"

if stack_exists $SERVICES_STACK_NAME; then
    echo "Updating existing Services stack..."
    aws cloudformation update-stack \
        --stack-name $SERVICES_STACK_NAME \
        --template-body file://cloudformation/ecs-services.yaml \
        --parameters \
            ParameterKey=EnvironmentName,ParameterValue=$ENVIRONMENT_NAME \
            ParameterKey=ECRRepository,ParameterValue=$ECR_REPOSITORY_URI \
            ParameterKey=ImageTag,ParameterValue=$IMAGE_TAG \
        --region $AWS_REGION || echo "No changes to Services stack"
else
    echo "Creating new Services stack..."
    aws cloudformation create-stack \
        --stack-name $SERVICES_STACK_NAME \
        --template-body file://cloudformation/ecs-services.yaml \
        --parameters \
            ParameterKey=EnvironmentName,ParameterValue=$ENVIRONMENT_NAME \
            ParameterKey=ECRRepository,ParameterValue=$ECR_REPOSITORY_URI \
            ParameterKey=ImageTag,ParameterValue=$IMAGE_TAG \
        --region $AWS_REGION
fi

wait_for_stack $SERVICES_STACK_NAME

# Get Load Balancer DNS
ALB_DNS=$(aws cloudformation describe-stacks \
    --stack-name $ECS_STACK_NAME \
    --region $AWS_REGION \
    --query 'Stacks[0].Outputs[?OutputKey==`LoadBalancerDNS`].OutputValue' \
    --output text)

echo ""
echo "Deployment completed successfully!"
echo "Application Load Balancer DNS: $ALB_DNS"
echo "MVC Application: http://$ALB_DNS"
echo "API Application: http://$ALB_DNS/api"
echo "API Documentation: http://$ALB_DNS/api/swagger"
echo ""
echo "Database password stored in Parameter Store: /${ENVIRONMENT_NAME}/database/password"