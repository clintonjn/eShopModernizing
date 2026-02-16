# eShopPorted AWS Deployment Script
param(
    [string]$Region = "us-east-1"
)

Write-Host "Deploying eShopPorted to AWS ECS Fargate" -ForegroundColor Green

# Get AWS Account ID
$AWS_ACCOUNT_ID = (aws sts get-caller-identity --query Account --output text)
Write-Host "AWS Account ID: $AWS_ACCOUNT_ID"

# Step 1: Create ECR Repository
Write-Host "`nStep 1: Creating ECR Repository..." -ForegroundColor Yellow
aws ecr create-repository --repository-name eshop-ported --region $Region 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Repository already exists or error occurred" -ForegroundColor Gray
}

# Step 2: Build and Push Docker Image
Write-Host "`nStep 2: Building and pushing Docker image..." -ForegroundColor Yellow
aws ecr get-login-password --region $Region | docker login --username AWS --password-stdin "$AWS_ACCOUNT_ID.dkr.ecr.$Region.amazonaws.com"

Set-Location eShopLegacyMVCSolution
docker build -f eShopPorted/Dockerfile -t eshop-ported:latest --build-arg source=eShopPorted .
docker tag eshop-ported:latest "$AWS_ACCOUNT_ID.dkr.ecr.$Region.amazonaws.com/eshop-ported:latest"
docker push "$AWS_ACCOUNT_ID.dkr.ecr.$Region.amazonaws.com/eshop-ported:latest"
Set-Location ..

# Step 3: Create ECS Cluster
Write-Host "`nStep 3: Creating ECS Cluster..." -ForegroundColor Yellow
aws ecs create-cluster --cluster-name eshop-cluster --region $Region 2>$null

# Step 4: Create CloudWatch Log Group
Write-Host "`nStep 4: Creating CloudWatch Log Group..." -ForegroundColor Yellow
aws logs create-log-group --log-group-name /ecs/eshop-ported --region $Region 2>$null

# Step 5: Create ECS Task Execution Role
Write-Host "`nStep 5: Creating ECS Task Execution Role..." -ForegroundColor Yellow
$trustPolicy = @"
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "ecs-tasks.amazonaws.com"
      },
      "Action": "sts:AssumeRole"
    }
  ]
}
"@

$trustPolicy | Out-File -FilePath trust-policy.json -Encoding utf8
aws iam create-role --role-name ecsTaskExecutionRole --assume-role-policy-document file://trust-policy.json 2>$null
aws iam attach-role-policy --role-name ecsTaskExecutionRole --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy 2>$null
Remove-Item trust-policy.json

# Wait for role to propagate
Start-Sleep -Seconds 10

# Step 6: Update and Register Task Definition
Write-Host "`nStep 6: Registering Task Definition..." -ForegroundColor Yellow
$taskDef = Get-Content eshop-ported-task.json -Raw
$taskDef = $taskDef -replace "ACCOUNT_ID", $AWS_ACCOUNT_ID
$taskDef = $taskDef -replace "REGION", $Region
$taskDef | Out-File -FilePath eshop-ported-task-updated.json -Encoding utf8

aws ecs register-task-definition --cli-input-json file://eshop-ported-task-updated.json --region $Region

# Step 7: Get Default VPC and Subnets
Write-Host "`nStep 7: Getting VPC information..." -ForegroundColor Yellow
$VPC_ID = (aws ec2 describe-vpcs --filters "Name=isDefault,Values=true" --query "Vpcs[0].VpcId" --output text --region $Region)
$SUBNET_IDS = (aws ec2 describe-subnets --filters "Name=vpc-id,Values=$VPC_ID" --query "Subnets[*].SubnetId" --output text --region $Region)
$SUBNET_LIST = $SUBNET_IDS -split '\s+'

Write-Host "VPC ID: $VPC_ID"
Write-Host "Subnets: $($SUBNET_LIST -join ', ')"

# Step 8: Create Security Group
Write-Host "`nStep 8: Creating Security Group..." -ForegroundColor Yellow
$SG_ID = (aws ec2 create-security-group --group-name eshop-ported-sg --description "Security group for eShop Ported" --vpc-id $VPC_ID --query 'GroupId' --output text --region $Region 2>$null)

if ($LASTEXITCODE -ne 0) {
    # Security group might already exist, try to get it
    $SG_ID = (aws ec2 describe-security-groups --filters "Name=group-name,Values=eshop-ported-sg" "Name=vpc-id,Values=$VPC_ID" --query "SecurityGroups[0].GroupId" --output text --region $Region)
}

Write-Host "Security Group ID: $SG_ID"

# Allow HTTP traffic
aws ec2 authorize-security-group-ingress --group-id $SG_ID --protocol tcp --port 80 --cidr 0.0.0.0/0 --region $Region 2>$null

# Step 9: Create ECS Service
Write-Host "`nStep 9: Creating ECS Service..." -ForegroundColor Yellow
$serviceConfig = @"
{
  "cluster": "eshop-cluster",
  "serviceName": "eshop-ported-service",
  "taskDefinition": "eshop-ported-task",
  "desiredCount": 1,
  "launchType": "FARGATE",
  "networkConfiguration": {
    "awsvpcConfiguration": {
      "subnets": ["$($SUBNET_LIST[0])", "$($SUBNET_LIST[1])"],
      "securityGroups": ["$SG_ID"],
      "assignPublicIp": "ENABLED"
    }
  }
}
"@

$serviceConfig | Out-File -FilePath service-config.json -Encoding utf8
aws ecs create-service --cli-input-json file://service-config.json --region $Region

# Step 10: Get Public IP
Write-Host "`nStep 10: Waiting for task to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

$TASK_ARN = (aws ecs list-tasks --cluster eshop-cluster --service-name eshop-ported-service --query "taskArns[0]" --output text --region $Region)

if ($TASK_ARN -and $TASK_ARN -ne "None") {
    $ENI_ID = (aws ecs describe-tasks --cluster eshop-cluster --tasks $TASK_ARN --query "tasks[0].attachments[0].details[?name=='networkInterfaceId'].value" --output text --region $Region)
    $PUBLIC_IP = (aws ec2 describe-network-interfaces --network-interface-ids $ENI_ID --query "NetworkInterfaces[0].Association.PublicIp" --output text --region $Region)
    
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "Deployment Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Application URL: http://$PUBLIC_IP" -ForegroundColor Cyan
    Write-Host "`nNote: It may take 1-2 minutes for the application to fully start." -ForegroundColor Yellow
    Write-Host "`nTo view logs:" -ForegroundColor Yellow
    Write-Host "aws logs tail /ecs/eshop-ported --follow --region $Region" -ForegroundColor Gray
} else {
    Write-Host "`nService created but task not yet running. Check status with:" -ForegroundColor Yellow
    Write-Host "aws ecs describe-services --cluster eshop-cluster --services eshop-ported-service --region $Region" -ForegroundColor Gray
}

# Cleanup temp files
Remove-Item eshop-ported-task-updated.json -ErrorAction SilentlyContinue
Remove-Item service-config.json -ErrorAction SilentlyContinue

Write-Host "`nTo delete all resources later, run:" -ForegroundColor Yellow
Write-Host "aws ecs delete-service --cluster eshop-cluster --service eshop-ported-service --force --region $Region" -ForegroundColor Gray
Write-Host "aws ecs delete-cluster --cluster eshop-cluster --region $Region" -ForegroundColor Gray
