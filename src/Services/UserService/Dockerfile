FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared projects
COPY ["src/Shared/Common.Contracts/Common.Contracts.csproj", "Shared/Common.Contracts/"]
COPY ["src/Shared/Common.Infrastructure/Common.Infrastructure.csproj", "Shared/Common.Infrastructure/"]

# Copy service projects
COPY ["src/Services/UserService/UserService.API/UserService.API.csproj", "Services/UserService/UserService.API/"]
COPY ["src/Services/UserService/UserService.Domain/UserService.Domain.csproj", "Services/UserService/UserService.Domain/"]
COPY ["src/Services/UserService/UserService.Infrastructure/UserService.Infrastructure.csproj", "Services/UserService/UserService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Services/UserService/UserService.API/UserService.API.csproj"

# Copy everything else
COPY src/Shared/ Shared/
COPY src/Services/UserService/ Services/UserService/

# Build
WORKDIR "/src/Services/UserService/UserService.API"
RUN dotnet build "UserService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.API.dll"]