# AWS eShopPorted Complete Cleanup Script
# This will delete ALL resources and stop all charges

param(
    [string]$Region = "us-east-1"
)

Write-Host "========================================" -ForegroundColor Red
Write-Host "AWS Resource Cleanup Script" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host "This will DELETE all eShop resources from AWS" -ForegroundColor Yellow
Write-Host ""

$confirmation = Read-Host "Are you sure you want to delete everything? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "Cleanup cancelled." -ForegroundColor Yellow
    exit
}

Write-Host "`nStarting cleanup..." -ForegroundColor Green

# Step 1: Delete ECS Service
Write-Host "`n[1/7] Deleting ECS Service..." -ForegroundColor Cyan
aws ecs delete-service --cluster eshop-cluster --service eshop-ported-service --force --region $Region 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ ECS Service deleted" -ForegroundColor Green
} else {
    Write-Host "✗ Service not found or already deleted" -ForegroundColor Gray
}

# Wait for service to be deleted
Write-Host "  Waiting for service deletion..." -ForegroundColor Gray
Start-Sleep -Seconds 10

# Step 2: Delete ECS Cluster
Write-Host "`n[2/7] Deleting ECS Cluster..." -ForegroundColor Cyan
aws ecs delete-cluster --cluster eshop-cluster --region $Region 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ ECS Cluster deleted" -ForegroundColor Green
} else {
    Write-Host "✗ Cluster not found or already deleted" -ForegroundColor Gray
}

# Step 3: Delete ECR Repository
Write-Host "`n[3/7] Deleting ECR Repository..." -ForegroundColor Cyan
aws ecr delete-repository --repository-name eshop-ported --force --region $Region 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ ECR Repository deleted" -ForegroundColor Green
} else {
    Write-Host "✗ Repository not found or already deleted" -ForegroundColor Gray
}

# Step 4: Delete Security Group
Write-Host "`n[4/7] Deleting Security Group..." -ForegroundColor Cyan
aws ec2 delete-security-group --group-id sg-0fb889f8ff732138b --region $Region 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Security Group deleted" -ForegroundColor Green
} else {
    Write-Host "✗ Security Group not found or already deleted" -ForegroundColor Gray
}

# Step 5: Delete CloudWatch Log Group
Write-Host "`n[5/7] Deleting CloudWatch Log Group..." -ForegroundColor Cyan
aws logs delete-log-group --log-group-name /ecs/eshop-ported --region $Region 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ CloudWatch Log Group deleted" -ForegroundColor Green
} else {
    Write-Host "✗ Log Group not found or already deleted" -ForegroundColor Gray
}

# Step 6: Detach IAM Policy
Write-Host "`n[6/7] Detaching IAM Policy..." -ForegroundColor Cyan
aws iam detach-role-policy --role-name ecsTaskExecutionRole --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ IAM Policy detached" -ForegroundColor Green
} else {
    Write-Host "✗ Policy not found or already detached" -ForegroundColor Gray
}

# Step 7: Delete IAM Role
Write-Host "`n[7/7] Deleting IAM Role..." -ForegroundColor Cyan
aws iam delete-role --role-name ecsTaskExecutionRole 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ IAM Role deleted" -ForegroundColor Green
} else {
    Write-Host "✗ Role not found or already deleted" -ForegroundColor Gray
}

# Cleanup local files
Write-Host "`n[Bonus] Cleaning up local files..." -ForegroundColor Cyan
Remove-Item task-def.json -ErrorAction SilentlyContinue
Remove-Item service.json -ErrorAction SilentlyContinue
Remove-Item trust-policy.json -ErrorAction SilentlyContinue
Remove-Item eshop-ported-task.json -ErrorAction SilentlyContinue
Remove-Item eshop-ported-task-updated.json -ErrorAction SilentlyContinue
Remove-Item service-config.json -ErrorAction SilentlyContinue
Write-Host "✓ Local files cleaned" -ForegroundColor Green

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "Cleanup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "All AWS resources have been deleted." -ForegroundColor White
Write-Host "You will no longer be charged for these resources." -ForegroundColor White
Write-Host ""
Write-Host "To verify, check your AWS Console:" -ForegroundColor Yellow
Write-Host "- ECS: https://console.aws.amazon.com/ecs/" -ForegroundColor Gray
Write-Host "- ECR: https://console.aws.amazon.com/ecr/" -ForegroundColor Gray
Write-Host "- IAM: https://console.aws.amazon.com/iam/" -ForegroundColor Gray
Write-Host ""
}
