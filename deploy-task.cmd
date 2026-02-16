@echo off
echo Registering ECS Task Definition...
aws ecs register-task-definition --family eshop-ported-task --network-mode awsvpc --requires-compatibilities FARGATE --cpu 256 --memory 512 --execution-role-arn arn:aws:iam::850995570535:role/ecsTaskExecutionRole --container-definitions "[{\"name\":\"eshop-ported\",\"image\":\"850995570535.dkr.ecr.us-east-1.amazonaws.com/eshop-ported:latest\",\"portMappings\":[{\"containerPort\":80,\"protocol\":\"tcp\"}],\"environment\":[{\"name\":\"ASPNETCORE_ENVIRONMENT\",\"value\":\"Production\"},{\"name\":\"ASPNETCORE_URLS\",\"value\":\"http://+:80\"},{\"name\":\"UseMockData\",\"value\":\"true\"}],\"logConfiguration\":{\"logDriver\":\"awslogs\",\"options\":{\"awslogs-group\":\"/ecs/eshop-ported\",\"awslogs-region\":\"us-east-1\",\"awslogs-stream-prefix\":\"ecs\"}}}]" --region us-east-1

echo.
echo Creating ECS Service...
aws ecs create-service --cluster eshop-cluster --service-name eshop-ported-service --task-definition eshop-ported-task --desired-count 1 --launch-type FARGATE --network-configuration "awsvpcConfiguration={subnets=[subnet-0cfb4885be3f0a97b,subnet-0ced1ff67939b86f7],securityGroups=[sg-0fb889f8ff732138b],assignPublicIp=ENABLED}" --region us-east-1

echo.
echo Waiting 30 seconds for task to start...
timeout /t 30 /nobreak

echo.
echo Getting public IP address...
for /f "tokens=*" %%i in ('aws ecs list-tasks --cluster eshop-cluster --service-name eshop-ported-service --query "taskArns[0]" --output text --region us-east-1') do set TASK_ARN=%%i
for /f "tokens=*" %%i in ('aws ecs describe-tasks --cluster eshop-cluster --tasks %TASK_ARN% --query "tasks[0].attachments[0].details[?name=='networkInterfaceId'].value" --output text --region us-east-1') do set ENI_ID=%%i
for /f "tokens=*" %%i in ('aws ec2 describe-network-interfaces --network-interface-ids %ENI_ID% --query "NetworkInterfaces[0].Association.PublicIp" --output text --region us-east-1') do set PUBLIC_IP=%%i

echo.
echo ========================================
echo Deployment Complete!
echo ========================================
echo Your application URL: http://%PUBLIC_IP%
echo.
pause
