# API Gateway

The API Gateway serves as the single entry point for the distributed payment system, providing intelligent routing, distributed tracing, and monitoring.

## 🎯 Purpose

- **Single Entry Point**: Routes requests to all four payment services
- **Distributed Tracing**: OpenTelemetry instrumentation for end-to-end request tracing
- **Health Monitoring**: Real-time health checks for all backend services
- **Service Discovery**: Centralized service routing configuration

## 🚀 Services

The gateway routes requests to the following services:

| Service | Route Pattern | Backend URL | Purpose |
|---------|---------------|-------------|---------|
| **PaymentProcessing** | `/api/payments/*` | `https://localhost:5001` | Payment orchestration and coordination |
| **RiskAssessment** | `/api/risk/*` | `https://localhost:5031` | Real-time risk evaluation |
| **PaymentRouter** | `/api/router/*` | `https://localhost:5032` | Provider selection and routing |
| **BankAdapter** | `/api/bank/*` | `https://localhost:5033` | Bank provider integration |

## 📊 Distributed Tracing

### OpenTelemetry Setup

The API Gateway uses OpenTelemetry for comprehensive distributed tracing:

```csharp
// Service Information
Service Name: ApiGateway
Service Namespace: PaymentSystem
Service Type: api-gateway

// Trace Instrumentation
- ASP.NET Core HTTP requests/responses
- YARP Reverse Proxy calls
- HttpClient outbound calls
- Custom activity sources from all services
```

### Trace Flow

When a request hits the gateway, the trace flow is:

```
Client Request → API Gateway → PaymentProcessing → RiskAssessment
                                          ↓
                                     PaymentRouter
                                          ↓
                                      BankAdapter
```

### Trace Context Propagation

Each request carries OpenTelemetry trace context headers:
- `traceparent`: Trace identifier and span information
- `tracestate`: Vendor-specific trace data

This enables **end-to-end tracing** from the gateway through all microservices.

## 🔧 Configuration

### Service Routing

Routes are configured in `appsettings.json`:

```json
{
  "ReverseProxy": {
    "Routes": {
      "payment-processing-route": {
        "ClusterId": "payment-processing-cluster",
        "Match": { "Path": "/api/payments/{**catch-all}" }
      }
    },
    "Clusters": {
      "payment-processing-cluster": {
        "Destinations": {
          "destination1": { "Address": "https://localhost:5001" }
        }
      }
    }
  }
}
```

### OpenTelemetry Configuration

```json
{
  "OpenTelemetry": {
    "ServiceName": "ApiGateway",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

## 🏃 Running the Gateway

### Development

```bash
# Navigate to gateway directory
cd src/Services/ApiGateway

# Run the gateway
dotnet run

# Gateway will be available at:
# HTTPS: https://localhost:5000
# HTTP:  http://localhost:5005
# Swagger UI: https://localhost:5000/swagger
```

### Production

```bash
# Build for production
dotnet build --configuration Release

# Run production build
dotnet run --configuration Release
```

## 🧪 Testing the Gateway

### Test Payment Processing Flow

```bash
# Create a payment through the gateway
curl -X POST https://localhost:5000/api/payments \
  -H "Content-Type: application/json" \
  -d '{
    "merchantId": "merchant-123",
    "customerId": "customer-456",
    "amount": 100.00,
    "currency": "USD",
    "paymentMethodToken": "pm_token_visa"
  }'
```

### Test Health Check

```bash
# Check gateway and all backend services health
curl https://localhost:5000/health

# Expected response:
{
  "Status": "Healthy",
  "Services": [
    { "Name": "payment-processing", "Status": "Healthy" },
    { "Name": "risk-assessment", "Status": "Healthy" },
    { "Name": "payment-router", "Status": "Healthy" },
    { "Name": "bank-adapter", "Status": "Healthy" }
  ]
}
```

### Test Gateway Information

```bash
# Get gateway capabilities and configuration
curl https://localhost:5000/gateway/info
```

## 📈 Monitoring & Observability

### Logs

Logs are written to:
- **Console**: Real-time log output
- **File**: `logs/apigateway-{date}.txt` (daily rotation)

### Health Checks

Active health checks run every 10 seconds against all backend services:
- **Path**: `/health` on each service
- **Timeout**: 5 seconds
- **Interval**: 10 seconds

### Distributed Tracing

To view distributed traces:

1. **Start Jaeger** (or your preferred OTLP collector):
   ```bash
   docker run -d --name jaeger \
     -e COLLECTOR_OTLP_ENABLED=true \
     -p 4317:4317 \
     -p 16686:16686 \
     jaegertracing/all-in-one:latest
   ```

2. **Make requests through the gateway**

3. **View traces in Jaeger UI**: `http://localhost:16686`

### Example Trace Visualization

```
┌─────────────────────────────────────────────────────────────┐
│ API Gateway                                                 │
├─────────────────────────────────────────────────────────────┤
│ 1. POST /api/payments                                      │
│    ├─ Headers: traceparent=00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01 │
│    ├─ Duration: 2.5s                                       │
│    └─ Tags: http.method=POST, http.route=/api/payments     │
└─────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│ PaymentProcessing Service                                   │
├─────────────────────────────────────────────────────────────┤
│ 1. Create Payment                                          │
│    ├─ Duration: 2.3s                                       │
│    ├─ Risk Assessment: 500ms                               │
│    ├─ Payment Routing: 300ms                              │
│    └─ Bank Authorization: 1.2s                             │
└─────────────────────────────────────────────────────────────┘
```

## 🔐 Security Considerations

### Current State
- ⚠️ **Development Configuration**: CORS allows all origins
- ⚠️ **No Authentication**: Gateway currently accepts all requests
- ⚠️ **HTTP Development**: Supports HTTP in development mode

### Production Recommendations
- ✅ **Enable Authentication**: Add API keys, OAuth, or JWT authentication
- ✅ **HTTPS Only**: Disable HTTP in production
- ✅ **Rate Limiting**: Add rate limiting middleware
- ✅ **Input Validation**: Add request validation middleware
- ✅ **CORS Configuration**: Restrict CORS to specific origins
- ✅ **Service-to-Service Auth**: Add mTLS between gateway and services

## 🛠️ Troubleshooting

### Gateway Fails to Start

```bash
# Check if ports are available
netstat -an | grep 5000
netstat -an | grep 5005

# Check backend services are running
curl https://localhost:5001/health
curl https://localhost:5031/health
curl https://localhost:5032/health
curl https://localhost:5033/health
```

### Requests Failing with 502/503 Errors

```bash
# Check gateway logs
tail -f logs/apigateway-*.txt

# Verify backend service health
curl https://localhost:5000/health
```

### Traces Not Appearing

```bash
# Verify OTLP endpoint is accessible
curl http://localhost:4317

# Check OpenTelemetry configuration
cat appsettings.json | grep -A 10 OpenTelemetry

# Verify Jaeger is running
docker ps | grep jaeger
```

## 📝 Configuration Files

- `appsettings.json` - Production configuration
- `appsettings.Development.json` - Development overrides
- `Properties/launchSettings.json` - Launch profiles
- `Program.cs` - Application startup and middleware configuration

## 🎯 Next Steps

1. **Add Authentication**: Implement API key or OAuth authentication
2. **Add Rate Limiting**: Implement rate limiting middleware
3. **Add Caching**: Add response caching for GET requests
4. **Add Circuit Breaker**: Implement circuit breaker patterns
5. **Add Load Balancing**: Configure multiple destinations per service
6. **Add Metrics**: Add Prometheus metrics endpoint
7. **Add API Documentation**: Consolidate API documentation from all services

## 📚 Resources

- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [ASP.NET Core Middleware](https://docs.microsoft.com/aspnet/core/fundamentals/middleware)
