FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared projects
COPY ["src/Shared/Common.Contracts/Common.Contracts.csproj", "Shared/Common.Contracts/"]
COPY ["src/Shared/Common.Infrastructure/Common.Infrastructure.csproj", "Shared/Common.Infrastructure/"]

# Copy service projects
COPY ["src/Services/WalletService/WalletService.API/WalletService.API.csproj", "Services/WalletService/WalletService.API/"]
COPY ["src/Services/WalletService/WalletService.Domain/WalletService.Domain.csproj", "Services/WalletService/WalletService.Domain/"]
COPY ["src/Services/WalletService/WalletService.Infrastructure/WalletService.Infrastructure.csproj", "Services/WalletService/WalletService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Services/WalletService/WalletService.API/WalletService.API.csproj"

# Copy everything else
COPY src/Shared/ Shared/
COPY src/Services/WalletService/ Services/WalletService/

# Build
WORKDIR "/src/Services/WalletService/WalletService.API"
RUN dotnet build "WalletService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WalletService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WalletService.API.dll"]
