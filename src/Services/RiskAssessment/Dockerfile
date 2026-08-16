# =========================
# Build Stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy only project files first
COPY ["src/Services/RiskAssessment/RiskAssessment.csproj", "src/Services/RiskAssessment/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]

# Restore dependencies
RUN dotnet restore "src/Services/RiskAssessment/RiskAssessment.csproj"

# Copy remaining source code
COPY . .

# Publish
RUN dotnet publish \
    "src/Services/RiskAssessment/RiskAssessment.csproj" \
    -c Release \
    -o /app/publish

# =========================
# Runtime Stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "RiskAssessment.dll"]