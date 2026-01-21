#!/bin/bash

# Create main directories
mkdir -p .github/workflows
mkdir -p docs
mkdir -p src/{ApiGateway,Shared/{Common.Contracts,Common.Infrastructure}}
mkdir -p src/Services/{UserService,WalletService,PaymentService,NotificationService}
mkdir -p tests/{UserService.Tests,WalletService.Tests,PaymentService.Tests,IntegrationTests}
mkdir -p k8s
mkdir -p scripts

# Create .NET solution
dotnet new sln -n DigitalWallet

# Create UserService projects
cd src/Services/UserService
dotnet new webapi -n UserService.API --framework net8.0
dotnet new classlib -n UserService.Domain --framework net8.0
dotnet new classlib -n UserService.Infrastructure --framework net8.0
cd ../../..

# Create WalletService projects
cd src/Services/WalletService
dotnet new webapi -n WalletService.API --framework net8.0
dotnet new classlib -n WalletService.Domain --framework net8.0
dotnet new classlib -n WalletService.Infrastructure --framework net8.0
cd ../../..

# Create PaymentService projects
cd src/Services/PaymentService
dotnet new webapi -n PaymentService.API --framework net8.0
dotnet new classlib -n PaymentService.Domain --framework net8.0
dotnet new classlib -n PaymentService.Infrastructure --framework net8.0
cd ../../..

# Create NotificationService
cd src/Services/NotificationService
dotnet new webapi -n NotificationService.API --framework net8.0
dotnet new classlib -n NotificationService.Domain --framework net8.0
dotnet new classlib -n NotificationService.Infrastructure --framework net8.0
cd ../../..

# Create API Gateway
cd src/ApiGateway
dotnet new webapi -n ApiGateway --framework net8.0
cd ../..

# Create Shared libraries
cd src/Shared/Common.Contracts
dotnet new classlib -n Common.Contracts --framework net8.0
cd ../../..

cd src/Shared/Common.Infrastructure
dotnet new classlib -n Common.Infrastructure --framework net8.0
cd ../../..

# Create test projects
cd tests
dotnet new xunit -n UserService.Tests --framework net8.0
dotnet new xunit -n WalletService.Tests --framework net8.0
dotnet new xunit -n PaymentService.Tests --framework net8.0
dotnet new xunit -n IntegrationTests --framework net8.0
cd ..

# Add projects to solution
dotnet sln add src/Services/UserService/UserService.API/UserService.API.csproj
dotnet sln add src/Services/UserService/UserService.Domain/UserService.Domain.csproj
dotnet sln add src/Services/UserService/UserService.Infrastructure/UserService.Infrastructure.csproj

dotnet sln add src/Services/WalletService/WalletService.API/WalletService.API.csproj
dotnet sln add src/Services/WalletService/WalletService.Domain/WalletService.Domain.csproj
dotnet sln add src/Services/WalletService/WalletService.Infrastructure/WalletService.Infrastructure.csproj

dotnet sln add src/Services/PaymentService/PaymentService.API/PaymentService.API.csproj
dotnet sln add src/Services/PaymentService/PaymentService.Domain/PaymentService.Domain.csproj
dotnet sln add src/Services/PaymentService/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj

dotnet sln add src/Services/NotificationService/NotificationService.API/NotificationService.API.csproj
dotnet sln add src/Services/NotificationService/NotificationService.Domain/NotificationService.Domain.csproj
dotnet sln add src/Services/NotificationService/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj

dotnet sln add src/ApiGateway/ApiGateway/ApiGateway.csproj

dotnet sln add src/Shared/Common.Contracts/Common.Contracts/Common.Contracts.csproj
dotnet sln add src/Shared/Common.Infrastructure/Common.Infrastructure/Common.Infrastructure.csproj

dotnet sln add tests/UserService.Tests/UserService.Tests.csproj
dotnet sln add tests/WalletService.Tests/WalletService.Tests.csproj
dotnet sln add tests/PaymentService.Tests/PaymentService.Tests.csproj
dotnet sln add tests/IntegrationTests/IntegrationTests.csproj

echo "Project structure created successfully!"
