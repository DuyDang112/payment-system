# Risk Assessment Service

Real-time risk evaluation service for payment transactions, implementing configurable business rules to prevent fraud and reduce chargebacks.

## Features Implemented

### ✅ Phase 1: Foundation (Completed)
- **Project Dependencies**: Added NuGet packages for EF Core, PostgreSQL, FluentValidation, Serilog, Swagger, OpenTelemetry
- **Shared Infrastructure**: Result<T> pattern, Error types, IHandler, IApiEndpoint interfaces
- **Domain Models**: Complete domain model with enums, value objects, and aggregates
- **DbContext**: RiskAssessmentDbContext with snake_case naming convention
- **Entity Configurations**: EF Core configurations for all domain entities

### ✅ Phase 2: Rule Engine (Completed)
- **Rule Engine Core**: Priority-based evaluation with early exit for BLOCK actions
- **Rule Evaluators**:
  - Velocity Rule Evaluator
  - Blacklist Rule Evaluator
  - Amount Rule Evaluator
  - Geo Rule Evaluator
- **Velocity Checking**: In-memory implementation (Redis-ready)

### ✅ Phase 3: Application Layer (Completed)
- **EvaluateRisk Feature**:
  - POST `/api/risk/evaluate` endpoint
  - Request validation with FluentValidation
  - 500ms timeout protection with fail-open strategy
  - Whitelist/Blacklist checking
  - Merchant profile integration
  - Rule engine evaluation
  - Risk score calculation (0-100)
  - Decision making (APPROVE/REJECT/REVIEW)

### ✅ Phase 4: Configuration (Completed)
- **Program.cs**: Complete service configuration
- **Serilog**: Structured logging to console and file
- **Swagger/OpenAPI**: API documentation
- **CORS**: Configured for development
- **Database**: PostgreSQL connection string configuration

## Architecture

### Domain Model
```
Domain/
├── Models/
│   ├── RiskEvaluation.cs          # Root aggregate
│   ├── RiskRule.cs                 # Versioned rule entity
│   ├── MerchantRiskProfile.cs      # Merchant configuration
│   ├── BlacklistEntry.cs           # Blacklist management
│   ├── WhitelistEntry.cs           # Whitelist management
│   ├── RiskScore.cs                # Value object (0-100)
│   ├── RuleTrigger.cs              # Value object
│   ├── VelocityCheckResult.cs      # Value object
│   └── Enums (RiskDecision, RuleType, RuleAction, etc.)
└── Events/
    ├── IDomainEvent.cs
    ├── RiskEvaluationCompletedEvent.cs
    ├── RiskEvaluationFailedEvent.cs
    └── SuspiciousActivityDetectedEvent.cs
```

### Rule Engine
```
Infrastructure/RuleEngine/
├── IRuleEngine.cs                  # Rule evaluation interface
├── RuleEngine.cs                    # Core rule engine
├── IRuleEvaluator.cs               # Evaluator interface
├── VelocityRuleEvaluator.cs
├── BlacklistRuleEvaluator.cs
├── AmountRuleEvaluator.cs
├── GeoRuleEvaluator.cs
└── RuleConditions.cs               # Condition DTOs
```

### API Layer
```
Features/EvaluateRisk/
├── EvaluateRiskEndpoint.cs        # POST /api/risk/evaluate
├── IEvaluateRiskHandler.cs
├── EvaluateRiskHandler.cs          # Business logic
├── EvaluateRiskRequest.cs
├── EvaluateRiskResponse.cs
├── EvaluateRiskValidator.cs        # FluentValidation
└── EvaluateRiskMappingExtensions.cs
```

## API Endpoints

### POST /api/risk/evaluate
Evaluates payment risk in real-time.

**Request:**
```json
{
  "paymentId": "PAY-123",
  "merchantId": "MERCHANT-001",
  "customerId": "CUSTOMER-001",
  "customerEmail": "customer@example.com",
  "amount": 100.50,
  "currency": "USD",
  "countryCode": "US",
  "ipAddress": "192.168.1.1",
  "paymentMethodToken": "tok_12345"
}
```

**Response:**
```json
{
  "evaluationId": "eval-abc123",
  "paymentId": "PAY-123",
  "riskScore": 25,
  "decision": "APPROVE",
  "evaluatedAt": "2025-01-06T10:30:00Z",
  "ruleVersion": "1.0",
  "triggeredRules": [],
  "timeoutOccurred": false
}
```

## Business Rules Implemented

1. **Risk Score Calculation** (0-100)
   - Low Risk: < 50 → APPROVE
   - Medium Risk: 50-79 → REVIEW
   - High Risk: ≥ 80 → REJECT

2. **Whitelist Priority**
   - Whitelisted entities bypass all checks → Auto-APPROVE

3. **Blacklist Blocking**
   - Blacklisted entities are immediately rejected

4. **Rule Evaluation**
   - Rules evaluated by priority (ascending)
   - BLOCK action triggers immediate rejection
   - Score impacts accumulate

5. **Timeout Protection**
   - 500ms timeout enforced
   - Fail-open strategy (APPROVE with warning)

6. **Velocity Checking**
   - In-memory counter (4 window types)
   - Configurable limits per merchant

## Database Schema

### Tables (snake_case)
- `risk_evaluations` - Evaluation results
- `risk_rules` - Rule definitions with versioning
- `merchant_risk_profiles` - Merchant configurations
- `blacklist_entries` - Blacklist management
- `whitelist_entries` - Whitelist management

### Key Features
- Schema: `risk_assessment`
- Naming: snake_case for all tables/columns
- Indexes on lookup fields
- JSONB for rule conditions
- Versioning support for rules

## Configuration

### Connection Strings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=risk_assessment;Username=postgres;Password=postgres"
  }
}
```

### Serilog
- Console output
- File output: `logs/riskassessment-{Date}.txt`
- Structured logging with context

### Swagger/OpenAPI
- Available at root URL in development
- Includes XML documentation comments
- Tagged by feature

## Next Steps

### TODO (Remaining)
- [ ] Implement rule management endpoints (CRUD)
- [ ] Implement merchant profile management endpoints
- [ ] Add evaluation history/audit endpoints
- [ ] Implement outcome recording endpoint
- [ ] Add database migrations
- [ ] Seed default rules and profiles
- [ ] Implement event publishing (RabbitMQ/Kafka)
- [ ] Add Redis for production velocity checking
- [ ] Implement ML model evaluator (stub created)
- [ ] Add comprehensive unit tests
- [ ] Add integration tests
- [ ] Performance testing (< 500ms requirement)
- [ ] Docker configuration
- [ ] Health checks

### Database Setup
1. Create database: `CREATE DATABASE risk_assessment;`
2. Run migrations: `dotnet ef database update`
3. Seed initial data (rules, profiles)

### Running the Service
```bash
# Development
dotnet run --project src/Services/RiskAssessment

# Build
dotnet build src/Services/RiskAssessment

# Swagger UI: https://localhost:7001/swagger/index.html
# API Endpoint: https://localhost:7001/api/risk/evaluate
```

## Dependencies

- .NET 9.0
- PostgreSQL 14+
- EF Core 9.0
- FluentValidation 11.x
- Serilog 4.x
- Swashbuckle.AspNetCore 7.x

## Performance Requirements Met

- ✅ < 500ms evaluation timeout enforced
- ✅ Fail-open strategy on timeout
- ✅ Rule priority ordering
- ✅ Early exit on BLOCK actions
- ⏳ 1,000 evaluations/second (needs load testing)
- ⏳ Redis caching for merchant profiles (implementation needed)

## Security Considerations

- API endpoint authentication (to be added)
- Admin-only endpoints for management
- Tenant isolation for merchant profiles
- Audit trail for all evaluations
- Encrypted data at rest (PostgreSQL)

## BRD Compliance

This implementation follows the Risk Assessment Service BRD:
- ✅ Real-time risk evaluation
- ✅ Configurable business rules
- ✅ Risk score calculation (0-100)
- ✅ Accept/reject/review decisions
- ✅ Rule versioning support
- ✅ Velocity checking framework
- ✅ Blacklist/whitelist management
- ✅ Geographic checks
- ✅ 500ms timeout requirement
- ✅ Deterministic results
- ✅ Idempotent evaluations
- ✅ Fail-open on timeout

## License

Proprietary - Payment System Project
