# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Services/Authentication/Authentication.csproj", "Services/Authentication/"]
COPY ["Shared/Shared.csproj", "Shared/"]

RUN dotnet restore "Services/Authentication/Authentication.csproj"

# Copy everything and build
COPY . .

WORKDIR "/src/Services/Authentication"

RUN dotnet build "Authentication.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Authentication.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install certificates for IdentityServer4 (optional for production)
# RUN apt-get update && apt-get install -y ca-certificates

EXPOSE 5034
EXPOSE 5035

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Authentication.dll"]
