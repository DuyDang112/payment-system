# Bank Adapter Service

Payment provider integration abstraction layer that normalizes external provider responses, handles provider-specific protocols, retry logic, rate limiting, and request logging with PCI-DSS compliance.

## Features Implemented

### ✅ Phase 1: Foundation (Completed)
- **Project Dependencies**: NuGet packages for EF Core, PostgreSQL, FluentValidation, Serilog, OpenTelemetry, Polly
- **Shared Infrastructure**: Result<T> pattern, Error types, IHandler, IApiEndpoint interfaces
- **Domain Models**: Provider, ProviderRequestLog, enums (Operation, AuthType, ProviderType, etc.)
- **DbContext**: BankAdapterDbContext with snake_case naming convention
- **Configuration**: Serilog structured logging, OpenTelemetry tracing, Swagger documentation

### ✅ Phase 2: Provider Integration (Completed)
- **Provider Abstraction**: IProvider interface with Authorize, Capture, Refund, Void operations
- **Provider Factory**: Factory pattern for creating provider instances from database config
- **Stripe Provider**: Complete Stripe API integration with payment intents
- **Adyen Provider**: Placeholder implementation for Adyen
- **Bank Provider**: Placeholder implementation for bank transfers
- **Wallet Provider**: Placeholder implementation for digital wallets

### ✅ Phase 3: Cross-Cutting Concerns (Completed)
- **Rate Limiting**: Per-provider rate limiting with concurrent request management
- **Retry Logic**: Exponential backoff with jitter for transient failures
- **Request Logging**: PCI-DSS compliant logging with sensitive data sanitization
- **Error Normalization**: Provider-specific errors normalized to internal error types
- **Event Publishing**: Domain events for all payment operations

### ✅ Phase 4: Application Layer (Completed)
- **Authorize Feature**: POST /api/bank/authorize with validation, retry, and logging
- **Capture Feature**: POST /api/bank/capture with validation, retry, and logging
- **Refund Feature**: POST /api/bank/refund with validation, retry, and logging
- **Void Feature**: POST /api/bank/void with validation, retry, and logging
- **Health Check**: GET /api/bank/providers/{id}/health for provider monitoring

## Architecture

### Domain Model
```
Domain/
├── Provider.cs                       # Provider configuration aggregate
├── ProviderRequestLog.cs             # Request/Response log entity
└── Enums.cs                          # Operation, AuthType, ProviderType, ProviderResult
```

### Provider Integration
```
Infrastructure/Providers/
├── IProvider.cs                      # Provider interface
├── ProviderFactory.cs                # Factory for creating providers
├── StripeProvider.cs                 # Stripe implementation
├── AdyenProvider.cs                  # Adyen implementation
├── BankProvider.cs                   # Bank implementation
└── WalletProvider.cs                 # Wallet implementation
```

### Cross-Cutting Services
```
Infrastructure/
├── RateLimiting/
│   └── RateLimiter.cs                # Per-provider rate limiting
├── Retry/
│   └── RetryPolicy.cs                 # Retry with exponential backoff
├── Logging/
│   └── RequestLoggingService.cs      # PCI-DSS compliant logging
└── Events/
    ├── Events.cs                      # Domain events
    └── EventPublisher.cs              # Event publishing
```

### API Layer
```
Features/
├── Authorize/
│   ├── AuthorizeEndpoint.cs          # POST /api/bank/authorize
│   ├── IAuthorizeHandler.cs
│   ├── AuthorizeHandler.cs            # Business logic
│   ├── AuthorizeRequest.cs
│   ├── AuthorizeResponse.cs
│   └── AuthorizeValidator.cs          # FluentValidation
├── Capture/                          # Similar structure
├── Refund/                            # Similar structure
├── Void/                              # Similar structure
└── HealthCheck/                       # Provider health monitoring
```

## API Endpoints

### POST /api/bank/authorize
Authorizes a payment through a provider.

**Request:**
```json
{
  "paymentId": "pay_1234567890",
  "providerId": "stripe_prod_001",
  "amount": 100.00,
  "currency": "USD",
  "metadata": {
    "orderId": "ord_123456",
    "customerId": "cust_123456"
  }
}
```

**Response:**
```json
{
  "paymentId": "pay_1234567890",
  "providerId": "stripe_prod_001",
  "providerTransactionId": "pi_1234567890_abcd1234",
  "success": true,
  "metadata": {
    "status": "requires_capture",
    "client_secret": "pi_1234567890_secret_xxx"
  }
}
```

### POST /api/bank/capture
Captures a previously authorized payment.

**Request:**
```json
{
  "paymentId": "pay_1234567890",
  "providerId": "stripe_prod_001",
  "authorizationToken": "pi_1234567890_abcd1234",
  "amount": 100.00,
  "currency": "USD",
  "metadata": {
    "orderId": "ord_123456"
  }
}
```

### POST /api/bank/refund
Refunds a payment.

**Request:**
```json
{
  "paymentId": "pay_1234567890",
  "providerId": "stripe_prod_001",
  "authorizationToken": "pi_1234567890_abcd1234",
  "amount": 50.00,
  "currency": "USD",
  "metadata": {
    "reason": "customer_request"
  }
}
```

### POST /api/bank/void
Cancels a previously authorized payment.

**Request:**
```json
{
  "paymentId": "pay_1234567890",
  "providerId": "stripe_prod_001",
  "authorizationToken": "pi_1234567890_abcd1234",
  "amount": 100.00,
  "currency": "USD",
  "metadata": {}
}
```

### GET /api/bank/providers/{id}/health
Checks provider health status.

**Response:**
```json
{
  "providerId": "stripe_prod_001",
  "isHealthy": true,
  "message": "Provider is operational"
}
```

## Business Rules Implemented

### 1. Rate Limiting
- Per-provider rate limits enforced
- Max requests per second: Configurable per provider
- Max concurrent requests: Configurable per provider
- Queuing when rate limit hit
- Rate limit exceeded events published

### 2. Retry Logic
- Transient failures retried with exponential backoff
- Provider-specific retry limits respected
- Jitter added to retry delays
- Errors classified as retryable or fatal
- All retry attempts logged

### 3. Error Handling
- All provider errors normalized to internal types
- Provider-specific errors not exposed upstream
- Timeouts handled gracefully
- Sensitive data sanitized in logs

### 4. Request Logging (PCI-DSS)
- All provider requests logged (sanitized)
- Request/response duration tracked
- Request history maintained for debugging
- Cardholder data removed from logs
- Account numbers masked
- CVV/CVC codes masked
- PINs masked
- API keys/tokens masked

### 5. Event Publishing
- PaymentAuthorizationSucceeded/Failed events
- PaymentCaptureSucceeded/Failed events
- PaymentRefundSucceeded/Failed events
- ProviderRateLimitExceeded events
- ProviderHealthChanged events

## Database Schema

### Tables (snake_case)
- `providers` - Provider configuration with auth and retry settings
- `provider_request_logs` - Request/response log with sanitized data

### Key Features
- Schema: Public (no schema prefix)
- Naming: snake_case for all tables/columns
- JSONB for complex configuration (auth, retry, rate limits, timeouts)
- Indexed on frequently queried fields
- Check constraints for data integrity

### Provider Configuration
```json
{
  "integrationId": "unique-id",
  "providerId": "stripe_prod_001",
  "providerType": "Stripe",
  "apiVersion": "v1",
  "endpoint": "https://api.stripe.com",
  "authConfig": {
    "type": "ApiKey",
    "credentials": {"api_key": "sk_xxx"}
  },
  "rateLimits": {
    "maxRequestsPerSecond": 100,
    "maxConcurrentRequests": 10
  },
  "retryConfig": {
    "maxRetries": 3,
    "backoffMs": 1000,
    "retryableErrors": ["timeout", "rate_limit_exceeded"]
  },
  "timeouts": {
    "connectTimeoutMs": 5000,
    "readTimeoutMs": 30000
  },
  "supportedOperations": ["Authorize", "Capture", "Refund", "Void"]
}
```

## Configuration

### Connection Strings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bank_adapter_db;Username=postgres;Password=postgres"
  }
}
```

### Serilog
- Console output with structured logging
- Minimum level: Information (Debug in development)
- Enriched with service name and context

### OpenTelemetry
- Service name: bank-adapter
- Service version: 1.0.0
- ASP.NET Core instrumentation
- Console exporter for traces

### Swagger/OpenAPI
- Available at root URL in development
- Tagged by feature (BankAdapter, Health)
- Include XML documentation comments

## Next Steps

### TODO (Remaining)
- [ ] Create database migrations
- [ ] Seed provider configurations
- [ ] Implement actual Adyen API integration
- [ ] Implement actual Bank/Wallet integrations
- [ ] Add message broker for events (RabbitMQ/Kafka)
- [ ] Implement comprehensive unit tests
- [ ] Add integration tests with mock providers
- [ ] Performance testing for rate limiting
- [ ] Add Redis for distributed rate limiting
- [ ] Implement circuit breaker pattern
- [ ] Add provider management endpoints (CRUD)
- [ ] Implement request replay for debugging
- [ ] Add metrics dashboard

### Database Setup
1. Create database: `CREATE DATABASE bank_adapter_db;`
2. Run migrations: `dotnet ef database update`
3. Seed provider configurations

### Running the Service
```bash
# Development
dotnet run --project src/Services/BankAdapter

# Build
dotnet build src/Services/BankAdapter

# Swagger UI: http://localhost:8080/swagger/index.html
# API Endpoints: http://localhost:8080/api/bank/*
# Health Check: http://localhost:8080/health
```

## Dependencies

- .NET 9.0
- PostgreSQL 15+
- EF Core 9.0
- FluentValidation 11.x
- Serilog 9.x
- Polly 8.x
- Swashbuckle.AspNetCore 7.x
- OpenTelemetry 1.x

## Performance Requirements Met

- ✅ Provider timeout handling (default 30s)
- ✅ Connection pooling for providers
- ✅ Retry with exponential backoff
- ✅ Rate limiting enforced
- ✅ Request logging sanitized
- ⏳ p95 latency < 2s (needs production testing)
- ⏳ 95%+ provider success rate (needs monitoring)

## Security Considerations

- ✅ PCI-DSS compliant logging (no cardholder data)
- ✅ Sensitive data sanitization in logs
- ✅ Provider credentials encryption support
- ✅ Rate limiting to prevent abuse
- ✅ Timeout handling for network reliability
- ⏳ Credential vault integration (HashiCorp Vault/AWS Secrets Manager)
- ⏳ mTLS support for provider communication
- ⏳ API authentication for internal services

## BRD Compliance

This implementation follows the Bank Adapter Service BRD:
- ✅ Provider integration abstraction (REQ-BA-001 to REQ-BA-005)
- ✅ Retry logic with exponential backoff (REQ-BA-006 to REQ-BA-009)
- ✅ Rate limiting per provider (REQ-BA-010 to REQ-BA-012)
- ✅ Error normalization (REQ-BA-013 to REQ-BA-016)
- ✅ Request logging (REQ-BA-017 to REQ-BA-019)
- ✅ Provider timeout handling (NFR-BA-001, NFR-BA-002)
- ✅ Credentials encryption (NFR-BA-003, NFR-BA-004)
- ✅ Request/response log sanitization (NFR-BA-005, NFR-BA-006)
- ✅ Rate limit respect (BR-BA-001)
- ✅ Transient failure retry (BR-BA-002)
- ✅ Error normalization (BR-BA-003)
- ✅ No upstream exposure (BR-BA-004)
- ✅ Log sanitization (BR-BA-005)

## File Structure

```
src/Services/BankAdapter/
├── Domain/                         # Domain models and enums
│   ├── Provider.cs                # Provider configuration aggregate
│   ├── ProviderRequestLog.cs     # Request/Response log entity
│   └── Enums.cs                   # All enums
├── Infrastructure/
│   ├── Data/
│   │   ├── BankAdapterDbContext.cs
│   │   └── JsonConverters.cs    # JSON converters for complex types
│   ├── Providers/                 # Provider implementations
│   │   ├── IProvider.cs
│   │   ├── ProviderFactory.cs
│   │   ├── StripeProvider.cs
│   │   └── [Other Providers]
│   ├── RateLimiting/              # Rate limiting service
│   ├── Retry/                     # Retry policies
│   ├── Logging/                   # Request logging service
│   └── Events/                    # Event publishing
├── Features/                       # API features
│   ├── Authorize/
│   ├── Capture/
│   ├── Refund/
│   ├── Void/
│   ├── HealthCheck/
│   └── Shared/
│       ├── Routes/                # Route constants
│       └── Errors/                # Error definitions
├── Shared/                         # Shared infrastructure
│   ├── Result.cs                  # Result<T> pattern
│   ├── Error.cs
│   ├── IHandler.cs
│   ├── IApiEndpoint.cs
│   └── ProblemExtensions.cs       # HTTP problem extensions
├── Program.cs                      # Application configuration
├── appsettings.json
├── appsettings.Development.json
├── Dockerfile
├── BankAdapter.http               # API test requests
└── README.md
```

## Docker Support

```dockerfile
# Multi-stage build for production
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["BankAdapter.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build --no-restore -c Release
RUN dotnet publish --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BankAdapter.dll"]
```

## License

Proprietary - Payment System Project
