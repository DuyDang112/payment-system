# =========================
# Build Stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy only project files first
COPY ["src/Services/PaymentProcessing/PaymentProcessing.csproj", "src/Services/PaymentProcessing/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]

# Restore dependencies
RUN dotnet restore "src/Services/PaymentProcessing/PaymentProcessing.csproj"

# Copy remaining source code
COPY . .

# Publish
RUN dotnet publish \
    "src/Services/PaymentProcessing/PaymentProcessing.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# =========================
# Runtime Stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "PaymentProcessing.dll"]