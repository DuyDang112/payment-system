# DDD Bounded Contexts Analysis: Distributed Payment System

## Overview

This document provides a comprehensive Domain-Driven Design (DDD) analysis of the distributed payment system described in the article ["Observability in Distributed Payment Systems"](https://www.bhupeshkumar.blog/blogs/observability-distributed-payment-systems).

**System Architecture:**
```
1. Merchant
2. API Gateway
3. Payment Service
4. Risk Service
5. Payment Router
6. Bank Adapter
7. Ledger Service
8. Payment Event (broker)
9. Notification Worker
```

**Identified Bounded Contexts:** 8 distinct services

---

# Service 1: API Gateway

## Purpose

**Business Goals and Responsibilities:**
- Single entry point for all client requests
- Request routing and protocol translation
- Cross-cutting concerns centralization (auth, rate limiting, observability)

**Problems Solved:**
- Prevents direct exposure of internal services
- Centralizes authentication and authorization
- Provides unified API surface for clients
- Enables request/response transformation

**Why It Should Be Its Own Bounded Context:**
- Infrastructure/operational concerns separate from business logic
- Single responsibility for traffic management
- Shared concern across all downstream services

## Domain Responsibilities

**Core Business Capabilities:**
- Request validation and schema enforcement
- Authentication and authorization enforcement
- Rate limiting and throttling
- Request routing to backend services
- Response aggregation
- Protocol translation (REST → gRPC/GraphQL if needed)

**Invariants and Business Rules:**
- All requests must be authenticated (except public health endpoints)
- Rate limits must be enforced per client
- Requests exceeding size limits must be rejected
- Failed authentication must never reach backend services

**Data Ownership Boundaries:**
- No persistent data ownership (stateless service)
- Temporary request context only

## Domain Entities

**No domain entities** - this is a stateless infrastructure service with only transient request context.

## Value Objects

| Value Object | Purpose | Properties |
|---|---|---|
| `AuthenticationContext` | Represents authenticated client | `userId`, `merchantId`, `scopes`, `expiresAt` |
| `RateLimitConfig` | Rate limit rules per client | `requestsPerMinute`, `burstCapacity`, `windowSize` |
| `RouteDefinition` | Service routing configuration | `pathPattern`, `targetService`, `httpMethod`, `timeoutMs` |

## Domain Events

**Events Published:**
- `ApiGatewayRequestReceived` - On incoming request (observability)
- `ApiGatewayRequestCompleted` - On request completion (observability)

**Events Consumed:**
- None (purely reactive)

## External Interfaces

### API Endpoints

| Endpoint | HTTP Method | Request Model | Response Model | Authorization |
|---|---|---|---|---|
| `/api/v1/payments` | POST | `CreatePaymentRequest` | `CreatePaymentResponse` | Bearer token (merchant) |
| `/api/v1/payments/{id}` | GET | N/A | `GetPaymentResponse` | Bearer token (merchant) |
| `/api/v1/health` | GET | N/A | `HealthStatus` | Public |
| `/api/v1/webhooks/*` | POST | `WebhookEvent` | `AckResponse` | Signature verification |

### Events Published

| Event Name | When Published | Consumers |
|---|---|---|
| `ApiGatewayRequestReceived` | On incoming request | Observability platform |
| `ApiGatewayRequestCompleted` | On request completion | Observability platform |

### Events Consumed

None

## Integrations

**Upstream Dependencies:**
- Client applications (web, mobile)
- External webhook providers

**Downstream Dependencies:**
- Payment Processing Service
- Merchant Service (for auth validation)
- Identity Provider (for token validation)

**Third-Party Systems:**
- OAuth/OIDC provider
- CDN (if deployed at edge)

## Data Storage

**Database Ownership:**
- None (stateless service)

**Read and Write Patterns:**
- In-memory caching for rate limit counters
- In-memory route table
- No persistent storage

## Service Boundaries

**What Belongs Inside:**
- Request validation
- Authentication/authorization enforcement
- Rate limiting
- Request routing
- Response transformation
- API versioning
- CORS handling

**What Must Not Belong Inside:**
- Business logic (payment processing, risk evaluation, etc.)
- Persistent data storage
- Long-running operations

**Justification:**
- Gateway is an infrastructure concern, not a business domain
- Adding business logic creates tight coupling
- Must remain lightweight and fast

## Communication with Other Services

**Synchronous Communication:**
- Payment Processing Service (HTTP/gRPC)
- Merchant Service (HTTP/gRPC for token validation)

**Asynchronous Communication:**
- None

**Required Contracts:**
- Service discovery mechanism
- Retry policies
- Circuit breaker configuration
- Timeout configurations per downstream service

## Suggested Microservice Structure

```
api-gateway/
├── app/
│   ├── application/          # Application layer
│   │   ├── handlers/         # Request handlers
│   │   ├── middleware/       # Auth, rate limiting
│   │   └── routing/          # Route configuration
│   ├── domain/               # Minimal domain (value objects)
│   ├── infrastructure/       # Infrastructure layer
│   │   ├── http/             # HTTP client/server
│   │   ├── auth/             # Authentication providers
│   │   └── observability/    # Logging, metrics
│   └── interfaces/           # API definitions
└── config/
    └── routes.yaml
```

## Implementation Notes

**CQRS Considerations:**
- Not applicable (stateless service)

**Event-Driven Architecture:**
- Emits observability events only
- No business event consumption

**Scalability Concerns:**
- Horizontal scaling via load balancer
- In-memory rate limiting requires consistent hashing or shared state
- Must handle high throughput with low latency

**Security Considerations:**
- All requests must be authenticated
- Never forward raw credentials to downstream services
- Implement request size limits to prevent DoS
- Validate all inputs before routing

---

# Service 2: Payment Processing Service

## Purpose

**Business Goals and Responsibilities:**
- Orchestrate the complete payment lifecycle
- Coordinate between multiple bounded contexts
- Ensure payment state consistency and correctness

**Problems Solved:**
- Complex multi-step payment coordination
- State management across distributed services
- Payment workflow orchestration
- Idempotency guarantees for payment operations

**Why It Should Be Its Own Bounded Context:**
- Payment orchestration is a core business domain
- Requires its own transactional boundaries
- Owns the payment aggregate lifecycle
- Coordination logic is distinct from actual processing (risk, routing, ledger)

## Domain Responsibilities

**Core Business Capabilities:**
- Payment creation and initialization
- Payment workflow orchestration
- Coordination of risk evaluation, routing, and execution
- Payment state management (created → processing → completed/failed)
- Idempotency enforcement
- Payment reconciliation triggers

**Invariants and Business Rules:**
- A payment can only transition to next state if current state is valid
- A completed payment cannot be modified (immutable after completion)
- Idempotency keys must be unique per merchant
- Payment amount cannot be negative
- Currency must be supported
- Payment must pass risk check before routing

**Data Ownership Boundaries:**
- **Owns:** Payment aggregate, payment state transitions, idempotency keys
- **Does not own:** Risk evaluation results, ledger entries, bank transaction details

## Domain Entities

### Entity: Payment

**Description:** The aggregate root representing a payment through its entire lifecycle.

**Key Attributes:**
```typescript
{
  paymentId: string;              // Unique identifier
  merchantId: string;             // Owning merchant
  customerId: string;             // Customer identifier
  amount: Money;                  // Value object
  currency: Currency;             // Value object
  status: PaymentStatus;          // Enum: CREATED, PROCESSING, RISK_EVALUATION, ROUTING, AUTHORIZING, COMPLETED, FAILED, CANCELLED
  idempotencyKey: string;         // For duplicate prevention
  createdAt: DateTime;
  updatedAt: DateTime;
  completedAt?: DateTime;
  failureReason?: string;
  metadata: Record<string, any>;
}
```

**Relationships:**
- One Payment → Many PaymentStateTransitions (event log)
- One Payment → One PaymentMethod (value object)
- One Payment → Many PaymentAttempts (retry history)

**Aggregate Root:** Yes - This is the aggregate root. All operations go through Payment.

### Entity: PaymentAttempt

**Description:** Represents a single attempt to process a payment (for retries).

**Key Attributes:**
```typescript
{
  attemptId: string;
  paymentId: string;              // Reference to Payment
  attemptNumber: number;
  startedAt: DateTime;
  completedAt?: DateTime;
  status: AttemptStatus;
  failureReason?: string;
  provider: string;               // Which bank/provider was tried
}
```

**Relationships:**
- Belongs to Payment aggregate

### Entity: PaymentStateTransition

**Description:** Event log of state transitions (audit trail).

**Key Attributes:**
```typescript
{
  transitionId: string;
  paymentId: string;
  fromStatus: PaymentStatus;
  toStatus: PaymentStatus;
  transitionedAt: DateTime;
  reason?: string;
}
```

**Relationships:**
- Belongs to Payment aggregate (audit log)

## Value Objects

| Value Object | Purpose | Properties |
|---|---|---|
| `Money` | Represent monetary amounts | `amount: Decimal`, `currency: string` |
| `Currency` | Validated currency code | `code: string` (ISO 4217), `decimalPlaces: number` |
| `PaymentMethod` | Payment method details | `type: CARD \| BANK_TRANSFER \| WALLET`, `details: encrypted` |
| `PaymentStatus` | Type-safe payment status | `value: enum`, `transitions: allowed[]` |
| `IdempotencyKey` | Duplicate prevention | `key: string`, `merchantId: string`, `expiresAt: DateTime` |

## Domain Events

| Event Name | Trigger Conditions | Event Payload |
|---|---|---|
| `PaymentCreated` | New payment initialized | `paymentId`, `merchantId`, `amount`, `currency`, `idempotencyKey` |
| `PaymentRiskEvaluationRequested` | Ready for risk check | `paymentId`, `amount`, `customerId`, `paymentMethod` |
| `PaymentRiskApproved` | Risk service approved | `paymentId`, `riskScore`, `evaluatedAt` |
| `PaymentRiskRejected` | Risk service rejected | `paymentId`, `reason`, `riskRules` |
| `PaymentRoutingRequested` | Ready for routing | `paymentId`, `amount`, `currency`, `paymentMethod` |
| `PaymentAuthorizationRequested` | Sent to bank adapter | `paymentId`, `provider`, `amount`, `authorizationData` |
| `PaymentCompleted` | Payment successful | `paymentId`, `amount`, `provider`, `completedAt` |
| `PaymentFailed` | Payment failed | `paymentId`, `failureReason`, `failedAt` |

## External Interfaces

### API Endpoints

| Endpoint | HTTP Method | Request Model | Response Model | Authorization |
|---|---|---|---|---|
| `/api/payments` | POST | `CreatePaymentRequest` | `CreatePaymentResponse` | Bearer token (merchant) |
| `/api/payments/{id}` | GET | N/A | `GetPaymentResponse` | Bearer token (merchant) |
| `/api/payments/{id}/cancel` | POST | `CancelPaymentRequest` | `CancelPaymentResponse` | Bearer token (merchant) |

### Events Published

| Event Name | When Published | Consumers |
|---|---|---|
| `PaymentCreated` | On payment creation | Ledger Service, Notification Service |
| `PaymentRiskApproved` | On risk approval | Payment Router Service |
| `PaymentRiskRejected` | On risk rejection | Notification Service, Ledger Service |
| `PaymentCompleted` | On successful completion | Ledger Service, Notification Service, Reconciliation Service |
| `PaymentFailed` | On payment failure | Notification Service, Ledger Service |

### Events Consumed

| Event Name | Producer | Processing Logic |
|---|---|---|
| `RiskEvaluationCompleted` | Risk Service | Update payment status, proceed to routing or rejection |
| `PaymentAuthorized` | Bank Adapter | Mark payment as completed, trigger finalization |
| `PaymentAuthorizationFailed` | Bank Adapter | Mark payment as failed or retry |
| `LedgerEntryCreated` | Ledger Service | Confirm payment finalization |

## Integrations

**Upstream Dependencies:**
- API Gateway
- Merchant Service (validation)

**Downstream Dependencies:**
- Risk Service (sync or async)
- Payment Router Service (async)
- Bank Adapter Service (async via router)
- Ledger Service (async)
- Notification Service (async)

**Third-Party Systems:**
- None directly (accessed via Bank Adapter)

## Data Storage

**Database Ownership:**
- Primary database for payment data
- Idempotency key store

**Tables/Collections:**
- `payments` - Payment aggregates
- `payment_attempts` - Retry history
- `payment_state_transitions` - Audit log
- `idempotency_keys` - Duplicate prevention

**Read and Write Patterns:**
- **Write:** Strong consistency required (ACID)
- **Read:** Eventual consistency acceptable for most queries
- **Hot data:** Recent payments (last 7 days)
- **Cold data:** Historical payments (archive after 90 days)

## Service Boundaries

**What Belongs Inside:**
- Payment lifecycle management
- Payment state machine
- Idempotency enforcement
- Payment orchestration logic
- Payment retry logic

**What Must Not Belong Inside:**
- Risk evaluation rules (belongs in Risk Service)
- Bank integration logic (belongs in Bank Adapter)
- Accounting/ledger entries (belongs in Ledger Service)
- Notification delivery (belongs in Notification Service)
- Routing logic (belongs in Payment Router)

**Justification:**
- Clear aggregate root (Payment)
- Risk evaluation is a separate bounded context with its own rules
- Ledger is a separate ubiquitous language (double-entry accounting)
- Bank integrations are external concerns

## Communication with Other Services

**Synchronous Communication:**
- Risk Service (if sync mode - could also be async)
- Payment Router (for routing decision)
- Merchant Service (for validation)

**Asynchronous Communication:**
- Ledger Service (domain events)
- Notification Service (domain events)
- Bank Adapter (via Payment Router)

**Required Contracts:**
- Risk evaluation request/response schema
- Payment routing request schema
- Domain event schemas
- Retry policy configuration

## Suggested Microservice Structure

```
payment-processing/
├── app/
│   ├── application/                  # Application layer
│   │   ├── commands/                 # Command handlers
│   │   │   ├── CreatePaymentHandler.ts
│   │   │   ├── ProcessPaymentHandler.ts
│   │   │   └── CancelPaymentHandler.ts
│   │   ├── queries/                  # Query handlers
│   │   │   ├── GetPaymentHandler.ts
│   │   │   └── ListPaymentsHandler.ts
│   │   ├── services/                 # Application services
│   │   │   ├── PaymentOrchestrator.ts
│   │   │   └── IdempotencyService.ts
│   │   └── events/                   # Event publishing
│   ├── domain/                       # Domain layer
│   │   ├── aggregates/               # Payment aggregate
│   │   │   ├── Payment.ts
│   │   │   ├── PaymentAttempt.ts
│   │   │   └── PaymentStateTransition.ts
│   │   ├── value-objects/            # Money, Currency, etc.
│   │   ├── services/                 # Domain services
│   │   │   ├── PaymentStateMachine.ts
│   │   │   └── PaymentDomainService.ts
│   │   └── events/                   # Domain events
│   │       ├── PaymentCreated.ts
│   │       ├── PaymentCompleted.ts
│   │       └── ...
│   ├── infrastructure/               # Infrastructure layer
│   │   ├── persistence/              # Repository implementations
│   │   │   ├── PaymentRepository.ts
│   │   │   └── IdempotencyRepository.ts
│   │   ├── messaging/                # Event bus implementation
│   │   │   ├── EventPublisher.ts
│   │   │   └── EventSubscriber.ts
│   │   ├── http/                     # HTTP clients for downstream services
│   │   └── observability/            # Tracing, metrics
│   └── interfaces/                   # API layer
│       ├── http/                     # REST controllers
│       └── dto/                      # Data transfer objects
└── config/
```

## Implementation Notes

**CQRS Considerations:**
- **Command side:** Strong consistency, ACID transactions
- **Query side:** Read models can be eventually consistent
- Consider separate read database for payment queries

**Event-Driven Architecture:**
- Publish domain events after state transitions
- Use saga pattern for distributed transaction coordination
- Event sourcing could be used for audit trail

**Scalability Concerns:**
- High write throughput (payments per second)
- Partition by paymentId or merchantId
- Idempotency key lookups must be fast (Redis cache)
- Hot data vs cold data separation

**Security Considerations:**
- Never store full payment card details
- Payment method details must be tokenized
- PII data encryption at rest
- Merchant data isolation (tenant separation)

---

# Service 3: Risk Assessment Service

## Purpose

**Business Goals and Responsibilities:**
- Evaluate payment transactions for fraud and risk
- Make accept/reject decisions based on business rules
- Protect merchants from fraudulent transactions
- Maintain risk evaluation consistency

**Problems Solved:**
- Fraud prevention
- Chargeback reduction
- Regulatory compliance (AML, KYC)
- Risk aggregation across merchants

**Why It Should Be Its Own Bounded Context:**
- Risk evaluation has its own ubiquitous language (risk scores, rules, thresholds)
- Business rules are complex and change frequently
- Risk evaluation is a distinct concern from payment processing
- Requires specialized data and models

## Domain Responsibilities

**Core Business Capabilities:**
- Real-time risk evaluation
- Fraud rule engine execution
- Risk score calculation
- Merchant risk profile management
- Risk rule configuration
- Risk event logging and audit

**Invariants and Business Rules:**
- Risk decision must be deterministic (same input → same output)
- Risk evaluation must complete within timeout (e.g., 500ms)
- High-risk transactions must be rejected
- Risk rules must be versioned
- Risk evaluation is idempotent (same request → same decision)

**Data Ownership Boundaries:**
- **Owns:** Risk rules, risk scores, merchant risk profiles, risk evaluation history
- **Does not own:** Payment details (references only), customer data

## Domain Entities

### Entity: RiskEvaluation

**Description:** Represents a single risk evaluation for a payment.

**Key Attributes:**
```typescript
{
  evaluationId: string;
  paymentId: string;
  merchantId: string;
  customerId: string;
  riskScore: number;              // 0-100
  riskDecision: RiskDecision;     // APPROVE, REJECT, REVIEW
  evaluatedAt: DateTime;
  ruleVersion: string;
  triggeredRules: RuleTrigger[];
  evaluationDetails: {
    velocityChecks: VelocityCheckResult;
    blacklistCheck: boolean;
    whitelistCheck: boolean;
    geoLocationCheck: GeoLocationResult;
    amountCheck: AmountCheckResult;
    customerProfile: CustomerRiskProfile;
  };
}
```

**Relationships:**
- References Payment (by paymentId)
- References MerchantRiskProfile (by merchantId)
- Has many RuleTrigger

**Aggregate Root:** Yes

### Entity: RiskRule

**Description:** A business rule for risk evaluation.

**Key Attributes:**
```typescript
{
  ruleId: string;
  ruleName: string;
  ruleType: RuleType;             // VELOCITY, BLACKLIST, AMOUNT, GEO, ML_MODEL
  version: number;
  isActive: boolean;
  priority: number;               // Higher evaluated first
  conditions: RuleCondition[];    // JSON rule logic
  action: RuleAction;             // BLOCK, FLAG, ADD_SCORE
  scoreImpact: number;
  createdAt: DateTime;
  updatedAt: DateTime;
}
```

**Relationships:**
- Independent entity (configuration)

**Aggregate Root:** Yes

### Entity: MerchantRiskProfile

**Description:** Risk profile and configuration for a merchant.

**Key Attributes:**
```typescript
{
  profileId: string;
  merchantId: string;
  riskLevel: RiskLevel;           // LOW, MEDIUM, HIGH
  riskThresholds: {
    autoRejectThreshold: number;
    manualReviewThreshold: number;
  };
  enabledRules: string[];         // Risk rule IDs
  whitelistedCountries: string[];
  blacklistedCountries: string[];
  maxTransactionAmount: Money;
  velocityLimits: VelocityLimit[];
  customRules: CustomRuleConfig[];
}
```

**Relationships:**
- One per Merchant
- Has many VelocityLimit

**Aggregate Root:** Yes

### Entity: RiskEvaluationHistory

**Description:** Historical log of risk evaluations for analytics and learning.

**Key Attributes:**
```typescript
{
  historyId: string;
  paymentId: string;
  merchantId: string;
  evaluationId: string;           // Reference to RiskEvaluation
  evaluationResult: RiskDecision;
  actualOutcome: Outcome;         // FRAUD, LEGIT, CHARGEBACK, REFUND
  evaluatedAt: DateTime;
  outcomeKnownAt: DateTime;
}
```

**Relationships:**
- References RiskEvaluation

**Aggregate Root:** No (part of RiskEvaluation aggregate or separate log)

## Value Objects

| Value Object | Purpose | Properties |
|---|---|---|
| `RiskScore` | Type-safe risk score | `value: 0-100`, `level: enum(LOW/MEDIUM/HIGH)` |
| `RuleTrigger` | Record of triggered rule | `ruleId`, `ruleName`, `action`, `scoreImpact` |
| `VelocityCheckResult` | Velocity check result | `isPassed: boolean`, `count: number`, `limit: number`, `window: string` |
| `GeoLocationResult` | Location check result | `country: string`, `isRisky: boolean`, `reason: string` |
| `AmountCheckResult` | Amount check result | `isPassed: boolean`, `amount: Money`, `threshold: Money` |

## Domain Events

| Event Name | Trigger Conditions | Event Payload |
|---|---|---|
| `RiskEvaluationRequested` | Payment service requests evaluation | `paymentId`, `merchantId`, `customerId`, `amount`, `paymentMethod` |
| `RiskEvaluationCompleted` | Evaluation completed | `paymentId`, `decision`, `riskScore`, `triggeredRules` |
| `RiskEvaluationFailed` | Evaluation error (timeout) | `paymentId`, `error`, `fallbackDecision` |
| `RiskRuleActivated` | Rule configuration changed | `ruleId`, `version`, `activatedAt` |
| `RiskRuleDeactivated` | Rule configuration changed | `ruleId`, `version`, `deactivatedAt` |
| `SuspiciousActivityDetected` | ML model or pattern match | `patternId`, `paymentIds`, `severity`, `details` |

## External Interfaces

### API Endpoints

| Endpoint | HTTP Method | Request Model | Response Model | Authorization |
|---|---|---|---|---|
| `/api/risk/evaluate` | POST | `EvaluateRiskRequest` | `EvaluateRiskResponse` | Internal service auth |
| `/api/risk/rules` | GET | N/A | `ListRulesResponse` | Admin role |
| `/api/risk/rules` | POST | `CreateRuleRequest` | `CreateRuleResponse` | Admin role |
| `/api/risk/merchants/{id}/profile` | GET | N/A | `GetMerchantProfileResponse` | Internal service auth |
| `/api/risk/merchants/{id}/profile` | PUT | `UpdateProfileRequest` | `UpdateProfileResponse` | Admin role |
| `/api/risk/evaluations/{id}` | GET | N/A | `GetEvaluationResponse` | Admin role |

### Events Published

| Event Name | When Published | Consumers |
|---|---|---|
| `RiskEvaluationCompleted` | On evaluation completion | Payment Processing Service, Analytics Service |
| `RiskEvaluationFailed` | On evaluation error | Payment Processing Service, Alerting Service |
| `SuspiciousActivityDetected` | On pattern detection | Fraud Investigation Team, Alerting Service |

### Events Consumed

| Event Name | Producer | Processing Logic |
|---|---|---|
| `PaymentCreated` | Payment Service | Trigger risk evaluation |
| `PaymentCompleted` | Payment Service | Update risk models (feedback) |
| `PaymentChargeback` | Ledger/Reconciliation Service | Update fraud models, mark as fraud |

## Integrations

**Upstream Dependencies:**
- Payment Processing Service
- Merchant Service (for merchant profiles)

**Downstream Dependencies:**
- Payment Processing Service (decision response)
- Analytics Service (evaluation history)
- Alerting Service (suspicious activity)

**Third-Party Systems:**
- ML model scoring services (if external)
- Fraud intelligence providers (optional)
- GeoIP services (for location checks)

## Data Storage

**Database Ownership:**
- Risk rules database
- Risk evaluation history
- Merchant risk profiles

**Tables/Collections:**
- `risk_rules` - Rule definitions
- `risk_evaluations` - Evaluation results
- `merchant_risk_profiles` - Merchant-specific risk config
- `risk_evaluation_history` - Historical log
- `suspicious_patterns` - Detected fraud patterns

**Read and Write Patterns:**
- **Write:** High frequency (every payment)
- **Read:** Frequent (evaluation queries)
- **Hot data:** Recent evaluations (last 30 days)
- **Cold data:** Historical evaluations for ML training

## Service Boundaries

**What Belongs Inside:**
- Risk evaluation logic
- Fraud rule engine
- Risk score calculation
- Risk rule configuration
- Merchant risk profile management

**What Must Not Belong Inside:**
- Payment processing logic (belongs in Payment Service)
- Bank integration logic (belongs in Bank Adapter)
- Ledger/accounting (belongs in Ledger Service)
- Customer management (references only)

**Justification:**
- Risk evaluation is a distinct ubiquitous language
- Business rules are independent of payment flow
- Can be evolved independently
- Different performance characteristics (may need ML models)

## Communication with Other Services

**Synchronous Communication:**
- Payment Processing Service (evaluate endpoint) - must be fast
- Merchant Service (profile lookup)

**Asynchronous Communication:**
- Analytics Service (evaluation history)
- Alerting Service (suspicious activity)

**Required Contracts:**
- Risk evaluation request schema
- Risk evaluation response schema
- Domain event schemas

## Suggested Microservice Structure

```
risk-assessment/
├── app/
│   ├── application/
│   │   ├── commands/
│   │   │   ├── EvaluateRiskHandler.ts
│   │   │   ├── CreateRuleHandler.ts
│   │   │   └── UpdateMerchantProfileHandler.ts
│   │   ├── queries/
│   │   │   ├── GetEvaluationHandler.ts
│   │   │   └── ListRulesHandler.ts
│   │   └── services/
│   │       ├── RuleEngine.ts
│   │       ├── RiskCalculator.ts
│   │       └── VelocityChecker.ts
│   ├── domain/
│   │   ├── aggregates/
│   │   │   ├── RiskEvaluation.ts
│   │   │   ├── RiskRule.ts
│   │   │   └── MerchantRiskProfile.ts
│   │   ├── value-objects/
│   │   ├── services/
│   │   │   ├── RiskDomainService.ts
│   │   │   └── FraudPatternDetectionService.ts
│   │   └── events/
│   ├── infrastructure/
│   │   ├── persistence/
│   │   ├── messaging/
│   │   ├── ml/
│   │   └── observability/
│   └── interfaces/
└── config/
```

## Implementation Notes

**CQRS Considerations:**
- Write side: Risk evaluations (ACID)
- Read side: Analytics and reporting (eventually consistent)
- Separate read database for historical queries

**Event-Driven Architecture:**
- Publish evaluation results for downstream services
- Subscribe to payment events for feedback loops
- Use event replay for model retraining

**Scalability Concerns:**
- Evaluation latency is critical (must be fast)
- Cache merchant risk profiles (Redis)
- Rule engine must be optimized
- Consider ML model serving infrastructure

**Security Considerations:**
- Risk evaluation data is sensitive (fraud patterns)
- Rule changes must be audited
- Merchant profile isolation (tenant separation)
- PII handling in evaluation data

---

# Service 4: Payment Router Service

## Purpose

**Business Goals and Responsibilities:**
- Route payments to appropriate payment providers/banks
- Provider selection based on cost, success rate, and availability
- Circuit breaking and failover between providers
- Optimize payment routing for business metrics

**Problems Solved:**
- Intelligent provider selection
- Cost optimization (routing to cheapest provider)
- Resilience (failover when providers fail)
- Load balancing across providers

**Why It Should Be Its Own Bounded Context:**
- Routing logic is complex and strategic
- Requires its own data (provider metrics, costs, configurations)
- Independent evolution (adding new providers, changing routing strategies)
- Cross-cutting concern across all payment methods

## Domain Responsibilities

**Core Business Capabilities:**
- Provider selection and ranking
- Cost-based routing optimization
- Circuit breaking and health checking
- Provider performance tracking
- Routing rule configuration
- Fallback and retry logic

**Invariants and Business Rules:**
- Must route to healthy providers only
- Must respect merchant provider preferences
- Must respect currency and payment method constraints
- Must have at least one fallback provider
- Routing must be deterministic (same input → same provider unless configured for randomization)

**Data Ownership Boundaries:**
- **Owns:** Provider configurations, routing rules, provider health metrics, routing decisions history
- **Does Not Own:** Payment details (references only), bank-specific APIs

## Domain Entities

### Entity: PaymentProvider

**Description:** Represents a payment provider/bank integration.

**Key Attributes:**
```typescript
{
  providerId: string;
  providerName: string;
  providerType: ProviderType;     // BANK, PAYMENT_GATEWAY, WALLET
  supportedCurrencies: string[];   // ISO codes
  supportedMethods: PaymentMethod[];
  priority: number;               // Higher = preferred
  isEnabled: boolean;
  costConfig: {
    fixedFee: Money;
    percentageFee: number;        // Percentage
    minFee?: Money;
    maxFee?: Money;
  };
  performanceMetrics: {
    successRate: number;          // 0-1
    p50Latency: number;           // ms
    p95Latency: number;
    p99Latency: number;
    dailyVolume: number;
    failureRate: number;
  };
  healthStatus: HealthStatus;     // HEALTHY, DEGRADED, UNHEALTHY
  lastHealthCheck: DateTime;
  circuitBreakerState: CircuitState; // CLOSED, OPEN, HALF_OPEN
}
```

**Relationships:**
- Many RoutingRules can reference this provider
- Many RoutingDecisions reference this provider

**Aggregate Root:** Yes

### Entity: RoutingRule

**Description:** Business rule for provider selection.

**Key Attributes:**
```typescript
{
  ruleId: string;
  ruleName: string;
  priority: number;
  conditions: {
    currency?: string;
    paymentMethod?: string;
    amountRange?: { min: Money, max: Money };
    merchantId?: string;
    customerCountry?: string;
  };
  action: RouteAction;              // USE_PROVIDER, FALLBACK_ORDER, RANDOM
  providers: string[];              // Provider IDs in priority order
  isEnabled: boolean;
}
```

**Relationships:**
- Independent entity (configuration)

**Aggregate Root:** Yes

### Entity: RoutingDecision

**Description:** Record of a routing decision (for audit and analytics).

**Key Attributes:**
```typescript
{
  decisionId: string;
  paymentId: string;
  selectedProviderId: string;
  alternativeProviderIds: string[]; // Fallback options
  routingStrategy: string;
  decisionReason: string;
  costEstimate: Money;
  decisionMadeAt: DateTime;
}
```

**Relationships:**
- References Payment
- References PaymentProvider

**Aggregate Root:** Yes

## Value Objects

| Value Object | Purpose | Properties |
|---|---|---|
| `ProviderCapability` | Provider capabilities | `currencies[]`, `methods[]`, `minAmount`, `maxAmount` |
| `ProviderHealth` | Provider health status | `status: enum`, `lastCheck: DateTime`, `failureCount: number` |
| `RoutingStrategy` | Strategy type | `type: enum(COST_BASED, PERFORMANCE, ROUND_ROBIN, PRIORITY)` |
| `CostCalculation` | Fee calculation | `fixedFee`, `percentageFee`, `totalCost` |

## Domain Events

| Event Name | Trigger Conditions | Event Payload |
|---|---|---|
| `PaymentRouteRequested` | Payment service requests routing | `paymentId`, `amount`, `currency`, `method`, `merchantId` |
| `PaymentRouteSelected` | Routing decision made | `paymentId`, `providerId`, `cost`, `strategy` |
| `ProviderHealthChanged` | Provider health status changes | `providerId`, `oldStatus`, `newStatus`, `reason` |
| `ProviderCircuitOpened` | Circuit breaker opens | `providerId`, `failureCount`, `openedAt` |
| `ProviderCircuitClosed` | Circuit breaker closes | `providerId`, `closedAt`, `successCount` |
| `ProviderAdded` | New provider configured | `providerId`, `providerName`, `capabilities` |
| `ProviderRemoved` | Provider removed | `providerId`, `removedAt`, `reason` |

## External Interfaces

### API Endpoints

| Endpoint | HTTP Method | Request Model | Response Model | Authorization |
|---|---|---|---|---|
| `/api/routing/route` | POST | `RoutePaymentRequest` | `RoutePaymentResponse` | Internal service auth |
| `/api/routing/providers` | GET | N/A | `ListProvidersResponse` | Admin role |
| `/api/routing/providers` | POST | `AddProviderRequest` | `AddProviderResponse` | Admin role |
| `/api/routing/providers/{id}` | PUT | `UpdateProviderRequest` | `UpdateProviderResponse` | Admin role |
| `/api/routing/providers/{id}/health` | GET | N/A | `ProviderHealthResponse` | Admin role |
| `/api/routing/rules` | GET | N/A | `ListRulesResponse` | Admin role |
| `/api/routing/rules` | POST | `CreateRuleRequest` | `CreateRuleResponse` | Admin role |

### Events Published

| Event Name | When Published | Consumers |
|---|---|---|
| `PaymentRouteSelected` | On routing decision | Payment Processing Service, Bank Adapter |
| `ProviderHealthChanged` | On health status change | Payment Processing Service, Alerting Service |
| `ProviderCircuitOpened` | On circuit breaker open | Alerting Service, Operations Team |
| `ProviderCircuitClosed` | On circuit breaker close | Alerting Service |

### Events Consumed

| Event Name | Producer | Processing Logic |
|---|---|---|
| `PaymentCreated` | Payment Service | Trigger routing (if pre-routing) |
| `PaymentAuthorizationFailed` | Bank Adapter | Update provider health, consider circuit breaker |
| `PaymentAuthorizationSucceeded` | Bank Adapter | Update provider health metrics |

## Integrations

**Upstream Dependencies:**
- Payment Processing Service
- Configuration Service (for provider configs)

**Downstream Dependencies:**
- Bank Adapter Service (providers)
- Payment Processing Service (routing decision)

**Third-Party Systems:**
- External payment provider APIs (for health checks only)

## Data Storage

**Database Ownership:**
- Provider configurations
- Routing rules
- Provider health metrics
- Routing decision history

**Tables/Collections:**
- `payment_providers` - Provider configurations
- `routing_rules` - Routing rules
- `routing_decisions` - Decision history
- `provider_health_metrics` - Time-series health data

**Read and Write Patterns:**
- **Write:** Provider updates, health updates, routing decisions
- **Read:** Frequent (every payment needs routing)
- **Hot data:** Provider configs (cache in Redis)
- **Cold data:** Routing decision history (analytics)

## Service Boundaries

**What Belongs Inside:**
- Provider selection logic
- Routing rule engine
- Circuit breaking
- Health checking
- Cost optimization
- Provider performance tracking

**What Must Not Belong Inside:**
- Payment processing (belongs in Payment Service)
- Bank API integration (belongs in Bank Adapter)
- Risk evaluation (belongs in Risk Service)
- Ledger/accounting (belongs in Ledger Service)

**Justification:**
- Routing is a distinct concern with its own logic
- Provider management is complex and independent
- Circuit breaking is a cross-cutting infrastructure concern
- Cost optimization is a business domain

## Communication with Other Services

**Synchronous Communication:**
- Payment Processing Service (routing endpoint)
- Bank Adapter Service (health checks, optional)

**Asynchronous Communication:**
- Alerting Service (provider health changes)

**Required Contracts:**
- Routing request schema
- Routing response schema
- Provider configuration schema

## Suggested Microservice Structure

```
payment-router/
├── app/
│   ├── application/
│   │   ├── commands/
│   │   │   ├── RoutePaymentHandler.ts
│   │   │   ├── AddProviderHandler.ts
│   │   │   └── UpdateProviderHandler.ts
│   │   ├── queries/
│   │   │   ├── ListProvidersHandler.ts
│   │   │   └── GetProviderHealthHandler.ts
│   │   └── services/
│   │       ├── RoutingEngine.ts
│   │       ├── ProviderSelector.ts
│   │       ├── CircuitBreakerService.ts
│   │       └── HealthChecker.ts
│   ├── domain/
│   │   ├── aggregates/
│   │   │   ├── PaymentProvider.ts
│   │   │   ├── RoutingRule.ts
│   │   │   └── RoutingDecision.ts
│   │   ├── value-objects/
│   │   ├── services/
│   │   │   └── RoutingDomainService.ts
│   │   └── events/
│   ├── infrastructure/
│   │   ├── persistence/
│   │   ├── messaging/
│   │   ├── cache/              # Redis for provider configs
│   │   └── observability/
│   └── interfaces/
└── config/
```

## Implementation Notes

**CQRS Considerations:**
- Write side: Provider updates, routing decisions (ACID)
- Read side: Provider lists, health status (cached, eventually consistent)
- Separate read cache for fast routing lookups

**Event-Driven Architecture:**
- Publish routing decisions
- Subscribe to payment events for feedback
- Circuit breaker state events

**Scalability Concerns:**
- Routing must be fast (sub-second latency)
- Cache provider configurations in Redis
- Health checks must not overload providers
- Routing decisions can be high throughput

**Security Considerations:**
- Provider credentials must be securely stored
- Routing rules may have business logic (protect against injection)
- Admin endpoints must be protected
- Audit trail for routing changes

---

# Service 5: Bank Adapter Service

## Purpose

**Business Goals and Responsibilities:**
- Abstract integration with external payment providers and banks
- Normalize provider responses into internal format
- Handle provider-specific protocols and APIs
- Provider resilience and retry logic

**Problems Solved:**
- Provider API fragmentation
- Protocol differences between providers
- Provider failure handling
- Rate limiting and throttling per provider

**Why It Should Be Its Own Bounded Context:**
- External integrations are an infrastructure concern
- Provider-specific logic should be isolated
- Each provider has its own "language" (API, protocols)
- Can be evolved independently (adding new providers)

## Domain Responsibilities

**Core Business Capabilities:**
- Provider API integration
- Request/response translation
- Authorization and capture handling
- Refund processing
- Provider-specific retry logic
- Provider rate limiting
- Provider error handling and normalization

**Invariants and Business Rules:**
- Must respect provider rate limits
- Must normalize all provider errors to internal error types
- Must not expose provider-specific errors to upstream services
- Must support idempotency for retried operations
- Must retry transient failures with exponential backoff
- Must handle provider timeouts gracefully

**Data Ownership Boundaries:**
- **Owns:** Provider configurations, provider credentials, rate limit state, retry state
- **Does Not Own:** Payment business logic, routing decisions, ledger entries

## Domain Entities

### Entity: ProviderIntegration

**Description:** Represents integration with a specific provider.

**Key Attributes:**
```typescript
{
  integrationId: string;
  providerId: string;            // References PaymentProvider
  providerName: string;
  apiVersion: string;
  endpoint: string;
  authConfig: {
    type: AuthType;              // API_KEY, OAUTH, MUTUAL_TLS
    credentials: EncryptedCredentials;
  };
  rateLimits: {
    maxRequestsPerSecond: number;
    maxConcurrentRequests: number;
  };
  retryConfig: {
    maxRetries: number;
    backoffMs: number;
    retryableErrors: string[];
  };
  timeouts: {
    connectTimeoutMs: number;
    readTimeoutMs: number;
  };
  isEnabled: boolean;
  supportedOperations: Operation[]; // AUTHORIZE, CAPTURE, REFUND, VOID
}
```

**Relationships:**
- One per PaymentProvider
- Has many ProviderRequestLog

**Aggregate Root:** Yes

### Entity: ProviderRequestLog

**Description:** Log of requests to a provider (audit and debugging).

**Key Attributes:**
```typescript
{
  logId: string;
  paymentId: string;
  providerId: string;
  operation: Operation;
  requestSentAt: DateTime;
  responseReceivedAt?: DateTime;
  duration?: number;             // ms
  requestPayload: string;        // Sanitized
  responsePayload: string;       // Sanitized
  httpStatusCode?: number;
  result: ProviderResult;        // SUCCESS, RETRYABLE_ERROR, FATAL_ERROR
  errorCode?: string;
  errorMessage?: string;
}
```

**Relationships:**
- Belongs to ProviderIntegration aggregate

**Aggregate Root:** No (part of ProviderIntegration)

## Value Objects

| Value Object | Purpose | Properties |
|---|---|---|
| `AuthorizationRequest` | Provider-specific auth request | `providerId`, `amount`, `currency`, `paymentMethod`, `customerId` |
| `AuthorizationResponse` | Normalized auth response | `success`, `providerTransactionId`, `responseCode`, `message`, `authorizedAmount` |
| `ProviderError` | Normalized error | `code`, `type`, `isRetryable`, `message` |
| `ProviderCredentials` | Encrypted credentials | `type`, `encryptedData`, `keyId` |

## Domain Events

| Event Name | Trigger Conditions | Event Payload |
|---|---|---|
| `PaymentAuthorizationRequested` | Received from payment router | `paymentId`, `providerId`, `amount`, `currency`, `paymentMethod` |
| `PaymentAuthorizationSucceeded` | Provider approved | `paymentId`, `providerId`, `providerTransactionId`, `authorizedAmount` |
| `PaymentAuthorizationFailed` | Provider declined or error | `paymentId`, `providerId`, `errorCode`, `errorMessage`, `isRetryable` |
| `PaymentCaptureRequested` | Capture authorized payment | `paymentId`, `providerId`, `providerTransactionId`, `amount` |
| `PaymentCaptureSucceeded` | Capture successful | `paymentId`, `providerId`, `providerCaptureId` |
| `PaymentCaptureFailed` | Capture failed | `paymentId`, `providerId`, `errorCode` |
| `PaymentRefundRequested` | Refund payment | `paymentId`, `providerId`, `amount`, `reason` |
| `PaymentRefundSucceeded` | Refund successful | `paymentId`, `providerId`, `providerRefundId` |
| `PaymentRefundFailed` | Refund failed | `paymentId`, `providerId`, `errorCode` |
| `ProviderRateLimitExceeded` | Provider rate limit hit | `providerId`, `rateLimit`, `retryAfter` |
| `ProviderHealthCheckFailed` | Health check failed | `providerId`, `error`, `failedAt` |

## External Interfaces

### API Endpoints

| Endpoint | HTTP Method | Request Model | Response Model | Authorization |
|---|---|---|---|---|
| `/api/bank/authorize` | POST | `AuthorizeRequest` | `AuthorizeResponse` | Internal service auth |
| `/api/bank/capture` | POST | `CaptureRequest` | `CaptureResponse` | Internal service auth |
| `/api/bank/refund` | POST | `RefundRequest` | `RefundResponse` | Internal service auth |
| `/api/bank/void` | POST | `VoidRequest` | `VoidResponse` | Internal service auth |
| `/api/bank/providers/{id}/health` | GET | N/A | `HealthCheckResponse` | Admin role |

### Events Published

| Event Name | When Published | Consumers |
|---|---|---|
| `PaymentAuthorizationSucceeded` | On successful authorization | Payment Processing Service, Ledger Service |
| `PaymentAuthorizationFailed` | On failed authorization | Payment Processing Service, Router Service |
| `PaymentCaptureSucceeded` | On successful capture | Payment Processing Service, Ledger Service |
| `PaymentRefundSucceeded` | On successful refund | Payment Processing Service, Ledger Service |
| `ProviderRateLimitExceeded` | On rate limit hit | Router Service, Alerting Service |

### Events Consumed

| Event Name | Producer | Processing Logic |
|---|---|---|
| `PaymentRouteSelected` | Router Service | Execute authorization with selected provider |
| `PaymentCaptureRequested` | Payment Service | Execute capture |
| `PaymentRefundRequested` | Payment Service | Execute refund |

## Integrations

**Upstream Dependencies:**
- Payment Processing Service
- Payment Router Service

**Downstream Dependencies:**
- External payment providers (banks, payment gateways)

**Third-Party Systems:**
- Multiple bank APIs
- Payment gateway APIs (Stripe, Adyen, etc.)
- Wallet providers (PayPal, etc.)

## Data Storage

**Database Ownership:**
- Provider integration configurations
- Request/response logs
- Rate limit state

**Tables/Collections:**
- `provider_integrations` - Integration configs
- `provider_request_logs` - Request history
- `provider_credentials` - Encrypted credentials (separate secure store)

**Read and Write Patterns:**
- **Write:** Request logs (high frequency)
- **Read:** Integration configs (frequent, cached)
- **Hot data:** Recent request logs (last 7 days)
- **Cold data:** Historical logs (archive after 90 days)

## Service Boundaries

**What Belongs Inside:**
- Provider API integration
- Request/response translation
- Provider retry logic
- Provider rate limiting
- Provider error normalization
- Provider health checking

**What Must Not Belong Inside:**
- Payment business logic (belongs in Payment Service)
- Routing decisions (belongs in Router Service)
- Risk evaluation (belongs in Risk Service)
- Ledger accounting (belongs in Ledger Service)
- Notification delivery (belongs in Notification Service)

**Justification:**
- External integration is an infrastructure concern
- Provider-specific logic should be isolated
- Normalization layer protects upstream from provider changes
- Retry and rate limiting are provider-specific

## Communication with Other Services

**Synchronous Communication:**
- Payment Processing Service (operations)
- Payment Router Service (health checks)

**Asynchronous Communication:**
- Payment Processing Service (events)
- Router Service (health updates)

**Required Contracts:**
- Normalized request/response schemas
- Provider error code mapping
- Domain event schemas

## Suggested Microservice Structure

```
bank-adapter/
├── app/
│   ├── application/
│   │   ├── commands/
│   │   │   ├── AuthorizePaymentHandler.ts
│   │   │   ├── CapturePaymentHandler.ts
│   │   │   └── RefundPaymentHandler.ts
│   │   ├── queries/
│   │   │   └── GetProviderHealthHandler.ts
│   │   └── services/
│   │       ├── ProviderAdapter.ts      // Abstract base
│   │       ├── ProviderFactory.ts      // Create provider-specific adapters
│   │       ├── RetryService.ts
│   │       └── RateLimiter.ts
│   ├── domain/
│   │   ├── aggregates/
│   │   │   ├── ProviderIntegration.ts
│   │   │   └── ProviderRequestLog.ts
│   │   ├── value-objects/
│   │   ├── services/
│   │   └── events/
│   ├── infrastructure/
│   │   ├── providers/                 // Provider-specific implementations
│   │   │   ├── stripe/
│   │   │   ├── adyen/
│   │   │   └── bank-a/
│   │   ├── persistence/
│   │   ├── http/
│   │   ├── cache/
│   │   └── observability/
│   └── interfaces/
└── config/
```

## Implementation Notes

**CQRS Considerations:**
- Write side: Request logs
- Read side: Analytics, debugging (eventually consistent)
- Separate read database for historical queries

**Event-Driven Architecture:**
- Publish operation results
- Subscribe to routing decisions
- Provider health events

**Scalability Concerns:**
- High outbound request rate
- Connection pooling to providers
- Rate limiting per provider
- Request timeouts must be configured per provider

**Security Considerations:**
- Provider credentials must be encrypted
- Credentials stored in secure vault (HashiCorp Vault, AWS Secrets Manager)
- Request/response logging must sanitize sensitive data
- No cardholder data in logs (PCI-DSS compliance)
- Mutual TLS for some providers

---

# Service 6: Ledger Service

## Purpose

**Business Goals and Responsibilities:**
- Maintain accurate financial records
- Double-entry bookkeeping
- Account balance management
- Reconciliation and audit trails
- Financial reporting

**Problems Solved:**
- Payment accounting and tracking
- Balance management across accounts
- Transactional consistency for financial operations
- Audit trail compliance
- Reconciliation with external providers

**Why It Should Be Its Own Bounded Context:**
- Accounting has its own ubiquitous language (debits, credits, accounts, journals)
- Strict invariants (double-entry, balance constraints)
- Regulatory compliance requirements
- Independent evolution (new account types, reporting needs)

## Domain Responsibilities

**Core Business Capabilities:**
- Double-entry transaction recording
- Account balance management
- Journal entry creation
- Financial transaction posting
- Account reconciliation
- Audit trail maintenance
- Financial reporting

**Invariants and Business Rules:**
- Every transaction must balance (debits = credits)
- Account balances must never go negative unless overdraft is enabled
- Posted transactions must be immutable
- Every journal entry must have audit trail
- Reconciliation must match external provider records
- Transactions must be posted atomically

**Data Ownership Boundaries:**
- **Owns:** Accounts, journal entries, ledger transactions, balances, audit logs
- **Does Not Own:** Payment details (references only), customer data

## Domain Entities

### Entity: Account

**Description:** Represents a financial account.

**Key Attributes:**
```typescript
{
  accountId: string;
  accountNumber: string;
  accountType: AccountType;       // ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE
  accountSubtype: AccountSubtype; // CASH, RECEIVABLE, PAYABLE, BANK, etc.
  ownerId: string;                // Merchant or customer ID
  currency: string;
  currentBalance: Money;
  availableBalance: Money;        // Balance minus pending holds
  isOverdraftAllowed: boolean;
  overdraftLimit?: Money;
  isActive: boolean;
  createdAt: DateTime;
  updatedAt: DateTime;
}
```

**Relationships:**
- Has many JournalEntryLines
- Has many AccountHolds

**Aggregate Root:** Yes

### Entity: JournalEntry

**Description:** Represents a complete journal entry (transaction).

**Key Attributes:**
```typescript
{
  entryId: string;
  entryDate: DateTime;
  entryNumber: string;           // Sequential or unique identifier
  referenceType: ReferenceType;  // PAYMENT, REFUND, CHARGEBACK, ADJUSTMENT
  referenceId: string;           // paymentId, refundId, etc.
  description: string;
  status: EntryStatus;           // PENDING, POSTED, REVERSED
  totalDebit: Money;
  totalCredit: Money;
  lines: JournalEntryLine[];     // At least 2 lines
  postedAt?: DateTime;
  createdBy: string;             // User or system
  createdAt: DateTime;
  updatedAt: DateTime;
}
```

**Relationships:**
- Has many JournalEntryLine (composition)
- References Payment (by referenceId)

**Aggregate Root:** Yes

### Entity: JournalEntryLine

**Description:** Individual line item in a journal entry.

**Key Attributes:**
```typescript
{
  lineId: string;
  entryId: string;               // Parent JournalEntry
  accountId: string;
  debitAmount: Money;
  creditAmount: Money;
  description: string;
  lineSequence: number;          // Order within entry
}
```

**Relationships:**
- Belongs to JournalEntry aggregate

**Aggregate Root:** No (part of JournalEntry)

### Entity: LedgerTransaction

**Description:** Posted transaction affecting account balances.

**Key Attributes:**
```typescript
{
  transactionId: string;
  entryId: string;               // References JournalEntry
  accountId: string;
  amount: Money;                 // Signed: positive for credit, negative for debit
  balanceBefore: Money;
  balanceAfter: Money;
  transactionDate: DateTime;
  postedAt: DateTime;
  referenceType: ReferenceType;
  referenceId: string;
}
```

**Relationships:**
- References JournalEntry
- References Account

**Aggregate Root:** No (created when JournalEntry is posted)

### Entity: Reconciliation

**Description:** Reconciliation record for matching with external providers.

**Key Attributes:**
```typescript
{
  reconciliationId: string;
  accountIds: string[];          // Accounts being reconciled
  periodStart: DateTime;
  periodEnd: DateTime;
  expectedBalance: Money;
  actualBalance: Money;
  difference: Money;
  status: ReconciliationStatus;  // PENDING, MATCHED, UNMATCHED
  discrepancies: Discrepancy[];
  reconciledBy: string;
  reconciledAt: DateTime;
}
```

**Relationships:**
- References Accounts
- Has many Discrepancy

**Aggregate Root:** Yes

## Value Objects

| Value Object | Purpose | Properties |
|---|---|---|
| `Money` | Monetary amount | `amount: Decimal`, `currency: string` |
| `AccountType` | Account classification | `type: enum(ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE)` |
| `AccountBalance` | Account balance | `current: Money`, `available: Money`, `pending: Money` |
| `EntryStatus` | Journal entry status | `status: enum(PENDING, POSTED, REVERSED)` |

## Domain Events

| Event Name | Trigger Conditions | Event Payload |
|---|---|---|
| `JournalEntryCreated` | New journal entry created | `entryId`, `referenceType`, `referenceId`, `totalDebit`, `totalCredit` |
| `JournalEntryPosted` | Entry posted to ledger | `entryId`, `postedAt`, `affectedAccounts` |
| `AccountBalanceChanged` | Account balance updated | `accountId`, `oldBalance`, `newBalance`, `transactionId` |
| `ReconciliationCompleted` | Reconciliation finished | `reconciliationId`, `period`, `status`, `discrepancies` |
| `ReconciliationFailed` | Reconciliation mismatch | `reconciliationId`, `expected`, `actual`, `difference` |
| `DiscrepancyDetected` | Unmatched transaction found | `discrepancyId`, `accountId`, `transactionId`, `details` |

## External Interfaces

### API Endpoints

| Endpoint | HTTP Method | Request Model | Response Model | Authorization |
|---|---|---|---|---|
| `/api/ledger/journal-entries` | POST | `CreateJournalEntryRequest` | `CreateJournalEntryResponse` | Internal service auth |
| `/api/ledger/journal-entries/{id}` | GET | N/A | `GetJournalEntryResponse` | Internal service auth |
| `/api/ledger/accounts/{id}` | GET | N/A | `GetAccountResponse` | Internal service auth |
| `/api/ledger/accounts/{id}/balance` | GET | N/A | `GetBalanceResponse` | Internal service auth |
| `/api/ledger/reconcile` | POST | `ReconcileRequest` | `ReconcileResponse` | Admin role |
| `/api/ledger/transactions` | GET | N/A | `ListTransactionsResponse` | Admin role |

### Events Published

| Event Name | When Published | Consumers |
|---|---|---|
| `JournalEntryPosted` | On entry posting | Payment Service, Reconciliation Service |
| `AccountBalanceChanged` | On balance update | Payment Service, Notification Service |
| `ReconciliationCompleted` | On reconciliation completion | Reporting Service, Audit Service |
| `ReconciliationFailed` | On reconciliation failure | Alerting Service, Operations Team |

### Events Consumed

| Event Name | Producer | Processing Logic |
|---|---|---|
| `PaymentCreated` | Payment Service | Create pending journal entry |
| `PaymentCompleted` | Payment Service | Post journal entry, update balances |
| `PaymentRefunded` | Payment Service | Create refund journal entry |
| `PaymentChargeback` | Payment Service | Create chargeback journal entry |

## Integrations

**Upstream Dependencies:**
- Payment Processing Service
- Bank Adapter Service (for external reconciliation)

**Downstream Dependencies:**
- Reporting Service
- Reconciliation Service
- Audit Service

**Third-Party Systems:**
- External bank APIs (for statement download)
- Accounting systems (QuickBooks, Xero integration)

## Data Storage

**Database Ownership:**
- Chart of accounts
- Journal entries
- Ledger transactions
- Account balances
- Reconciliation records

**Tables/Collections:**
- `accounts` - Account definitions
- `journal_entries` - Journal entries
- `journal_entry_lines` - Entry line items
- `ledger_transactions` - Posted transactions
- `account_balances` - Current balances (daily snapshots)
- `reconciliations` - Reconciliation records

**Read and Write Patterns:**
- **Write:** ACID transactions required for posting
- **Read:** Frequent balance queries, historical reporting
- **Hot data:** Recent transactions, current balances
- **Cold data:** Historical transactions (archive after 7 years)

## Service Boundaries

**What Belongs Inside:**
- Double-entry bookkeeping
- Account management
- Journal entry creation and posting
- Balance calculation
- Reconciliation logic
- Audit trail

**What Must Not Belong Inside:**
- Payment processing logic (belongs in Payment Service)
- Bank API integration (belongs in Bank Adapter)
- Risk evaluation (belongs in Risk Service)
- Customer data management (references only)
- Reporting and dashboards (belongs in Reporting Service)

**Justification:**
- Accounting is a distinct ubiquitous language
- Strict invariants (double-entry) must be protected
- Regulatory compliance requires separation
- Ledger is the source of truth for financial data

## Communication with Other Services

**Synchronous Communication:**
- Payment Processing Service (journal entry creation, balance queries)
- Reconciliation Service (reconciliation operations)

**Asynchronous Communication:**
- Reporting Service (ledger events)
- Audit Service (audit events)

**Required Contracts:**
- Journal entry schema
- Account balance schema
- Domain event schemas

## Suggested Microservice Structure

```
ledger/
├── app/
│   ├── application/
│   │   ├── commands/
│   │   │   ├── CreateJournalEntryHandler.ts
│   │   │   ├── PostJournalEntryHandler.ts
│   │   │   ├── CreateAccountHandler.ts
│   │   │   └── ReconcileHandler.ts
│   │   ├── queries/
│   │   │   ├── GetAccountBalanceHandler.ts
│   │   │   ├── GetJournalEntryHandler.ts
│   │   │   └── ListTransactionsHandler.ts
│   │   └── services/
│   │       ├── JournalEntryService.ts
│   │       ├── BalanceCalculator.ts
│   │       └── ReconciliationService.ts
│   ├── domain/
│   │   ├── aggregates/
│   │   │   ├── Account.ts
│   │   │   ├── JournalEntry.ts
│   │   │   ├── JournalEntryLine.ts
│   │   │   ├── LedgerTransaction.ts
│   │   │   └── Reconciliation.ts
│   │   ├── value-objects/
│   │   ├── services/
│   │   │   ├── DoubleEntryBookkeepingService.ts
│   │   │   └── AccountDomainService.ts
│   │   └── events/
│   ├── infrastructure/
│   │   ├── persistence/
│   │   ├── messaging/
│   │   └── observability/
│   └── interfaces/
└── config/
```

## Implementation Notes

**CQRS Considerations:**
- Write side: Journal entries, transactions (ACID)
- Read side: Balance queries, reporting (read replicas, materialized views)
- Separate read database for reporting

**Event-Driven Architecture:**
- Publish ledger events
- Subscribe to payment events
- Reconciliation events

**Scalability Concerns:**
- High transaction volume (payments per second)
- Balance calculation must be fast
- Journal entry posting must be atomic
- Partition by account or date

**Security Considerations:**
- Financial data must be encrypted at rest
- Strict access controls (role-based)
- Audit trail for all operations
- Immutable posted entries
- Compliance with financial regulations

---

# Service 7: Notification Service

## Purpose

**Business Goals and Responsibilities:**
- Deliver notifications to customers and merchants
- Support multiple notification channels (email, SMS, push)
- Manage notification templates and preferences
- Track notification delivery status

**Problems Solved:**
- Customer communication for payment events
- Merchant notifications
- Delivery reliability across channels
- Template management
- Preference management

**Why It Should Be Its Own Bounded Context:**
- Notification delivery is a distinct concern
- Multiple channels with different protocols
- Template management is complex
- Delivery tracking and retry logic is independent

## Domain Responsibilities

**Core Business Capabilities:**
- Notification template management
- Multi-channel delivery (email, SMS, push, webhook)
- Delivery status tracking
- Retry logic for failed notifications
- Notification preference management
- Batch notifications
- Notification scheduling

**Invariants and Business Rules:**
- Notifications must be delivered at-least-once
- Failed notifications must be retried with backoff
- User preferences must be respected
- Templates must be validated before use
- Rate limiting per channel (SMS, email)
- Notifications must not exceed size limits

**Data Ownership Boundaries:**
- **Owns:** Notification templates, notification logs, delivery status, user preferences
- **Does Not Own:** Payment details (references only), customer data (references only)

## Domain Entities

### Entity: Notification

**Description:** Represents a notification to be delivered.

**Key Attributes:**
```typescript
{
  notificationId: string;
  referenceType: ReferenceType;   // PAYMENT, REFUND, CHARGEBACK, SYSTEM
  referenceId: string;           // paymentId, etc.
  recipientType: RecipientType;  // CUSTOMER, MERCHANT, ADMIN
  recipientId: string;            // Customer or merchant ID
  channels: NotificationChannel[]; // EMAIL, SMS, PUSH, WEBHOOK
  templateId: string;
  templateData: Record<string, any>; // Variables for template
  status: NotificationStatus;     // PENDING, SENDING, SENT, FAILED, RETRYING
  priority: Priority;            // LOW, NORMAL, HIGH, URGENT
  scheduledFor?: DateTime;
  attempts: number;
  maxAttempts: number;
  nextRetryAt?: DateTime;
  createdAt: DateTime;
  sentAt?: DateTime;
  deliveredAt?: DateTime;
  failedAt?: DateTime;
  failureReason?: string;
}
```

**Relationships:**
- Has many NotificationDelivery (one per channel)
- References NotificationTemplate

**Aggregate Root:** Yes

### Entity: NotificationDelivery

**Description:** Represents delivery attempt for a specific channel.

**Key Attributes:**
```typescript
{
  deliveryId: string;
  notificationId: string;
  channel: NotificationChannel;
  recipientAddress: string;       // Email, phone, push token, webhook URL
  status: DeliveryStatus;         // PENDING, SENT, DELIVERED, FAILED, BOUNCED
  provider: string;                // SendGrid, Twilio, etc.
  providerMessageId?: string;     // External provider message ID
  attemptNumber: number;
  sentAt?: DateTime;
  deliveredAt?: DateTime;
  failedAt?: DateTime;
  failureReason?: string;
  errorCode?: string;
}
```

**Relationships:**
- Belongs to Notification aggregate

**Aggregate Root:** No (part of Notification)

### Entity: NotificationTemplate

**Description:** Template for notification content.

**Key Attributes:**
```typescript
{
  templateId: string;
  templateName: string;
  templateType: TemplateType;     // EMAIL, SMS, PUSH, WEBHOOK
  subject?: string;               // For email
  body: string;                   // Template with variables (e.g., {{payment.amount}})
  variables: string[];            // Required template variables
  language: string;               // ISO language code
  isActive: boolean;
  version: number;
  createdAt: DateTime;
  updatedAt: DateTime;
}
```

**Relationships:**
- Independent entity

**Aggregate Root:** Yes

### Entity: NotificationPreference

**Description:** User notification preferences.

**Key Attributes:**
```typescript
{
  preferenceId: string;
  userId: string;
  userType: UserType;             // CUSTOMER, MERCHANT
  enabledChannels: NotificationChannel[];
  notifications: {
    paymentSuccess: boolean;
    paymentFailure: boolean;
    refundReceived: boolean;
    chargebackReceived: boolean;
    marketingEmails: boolean;
    securityAlerts: boolean;
  };
  language: string;
  timezone: string;
  updatedAt: DateTime;
}
```

**Relationships:**
- One per user

**Aggregate Root:** Yes

## Value Objects

| Value Object | Purpose | Properties |
|---|---|---|
| `NotificationChannel` | Delivery channel | `type: enum(EMAIL, SMS, PUSH, WEBHOOK)` |
| `NotificationStatus` | Notification status | `status: enum(PENDING, SENDING, SENT, FAILED, RETRYING)` |
| `Priority` | Notification priority | `level: enum(LOW, NORMAL, HIGH, URGENT)` |
| `DeliveryStatus` | Delivery status | `status: enum(PENDING, SENT, DELIVERED, FAILED, BOUNCED)` |
| `RecipientAddress` | Recipient contact | `channel: enum`, `address: string`, `verified: boolean` |

## Domain Events

| Event Name | Trigger Conditions | Event Payload |
|---|---|---|
| `NotificationCreated` | New notification created | `notificationId`, `referenceType`, `referenceId`, `channels`, `priority` |
| `NotificationSent` | Notification sent successfully | `notificationId`, `channels`, `sentAt` |
| `NotificationDelivered` | Notification delivered to recipient | `notificationId`, `channel`, `deliveredAt` |
| `NotificationFailed` | Notification delivery failed | `notificationId`, `channel`, `failureReason`, `willRetry` |
| `NotificationRetryScheduled` | Retry scheduled | `notificationId`, `retryAt`, `attemptNumber` |
| `NotificationBounced` | Notification bounced (permanent failure) | `notificationId`, `channel`, `bounceReason` |

## External Interfaces

### API Endpoints

| Endpoint | HTTP Method | Request Model | Response Model | Authorization |
|---|---|---|---|---|
| `/api/notifications/send` | POST | `SendNotificationRequest` | `SendNotificationResponse` | Internal service auth |
| `/api/notifications/{id}` | GET | N/A | `GetNotificationResponse` | Internal service auth |
| `/api/notifications/{id}/retry` | POST | N/A | `RetryNotificationResponse` | Internal service auth |
| `/api/notifications/templates` | GET | N/A | `ListTemplatesResponse` | Admin role |
| `/api/notifications/templates` | POST | `CreateTemplateRequest` | `CreateTemplateResponse` | Admin role |
| `/api/notifications/users/{id}/preferences` | GET | N/A | `GetPreferencesResponse` | User auth |
| `/api/notifications/users/{id}/preferences` | PUT | `UpdatePreferencesRequest` | `UpdatePreferencesResponse` | User auth |

### Events Published

| Event Name | When Published | Consumers |
|---|---|---|
| `NotificationDelivered` | On successful delivery | Analytics Service |
| `NotificationFailed` | On permanent failure | Alerting Service (for critical notifications) |
| `NotificationBounced` | On bounce | Customer Service (update user contact info) |

### Events Consumed

| Event Name | Producer | Processing Logic |
|---|---|---|
| `PaymentCompleted` | Payment Service | Send payment success notification |
| `PaymentFailed` | Payment Service | Send payment failure notification |
| `PaymentRefunded` | Payment Service | Send refund notification |
| `PaymentChargeback` | Ledger/Payment Service | Send chargeback notification |
| `RiskRejected` | Risk Service | Send fraud alert (if applicable) |

## Integrations

**Upstream Dependencies:**
- Payment Processing Service
- Ledger Service
- Risk Service

**Downstream Dependencies:**
- Email providers (SendGrid, AWS SES, Mailgun)
- SMS providers (Twilio, SNS)
- Push notification services (FCM, APNS)
- Webhook delivery

**Third-Party Systems:**
- Email service providers
- SMS gateways
- Push notification platforms

## Data Storage

**Database Ownership:**
- Notification templates
- Notification records
- Delivery logs
- User preferences

**Tables/Collections:**
- `notifications` - Notification records
- `notification_deliveries` - Delivery attempts per channel
- `notification_templates` - Template definitions
- `notification_preferences` - User preferences

**Read and Write Patterns:**
- **Write:** Notification creation, delivery updates
- **Read:** Template queries, notification status checks
- **Hot data:** Recent notifications (last 30 days)
- **Cold data:** Historical notifications (archive after 1 year)

## Service Boundaries

**What Belongs Inside:**
- Notification template management
- Multi-channel delivery
- Delivery status tracking
- Retry logic
- Preference management
- Rate limiting per channel

**What Must Not Belong Inside:**
- Payment business logic (belongs in Payment Service)
- Risk evaluation (belongs in Risk Service)
- Ledger accounting (belongs in Ledger Service)
- Customer data management (references only)
- Email marketing campaigns (separate concern)

**Justification:**
- Notification delivery is a distinct ubiquitous language
- Different channels have different protocols
- Template management is complex and independent
- Delivery tracking is separate from business logic

## Communication with Other Services

**Synchronous Communication:**
- Minimal (mostly async event-driven)

**Asynchronous Communication:**
- Message broker for consuming payment events
- Message broker for delivery status updates

**Required Contracts:**
- Notification request schema
- Template schema
- Domain event schemas

## Suggested Microservice Structure

```
notification/
├── app/
│   ├── application/
│   │   ├── commands/
│   │   │   ├── SendNotificationHandler.ts
│   │   │   ├── CreateTemplateHandler.ts
│   │   │   └── UpdatePreferencesHandler.ts
│   │   ├── queries/
│   │   │   ├── GetNotificationHandler.ts
│   │   │   └── ListTemplatesHandler.ts
│   │   └── services/
│   │       ├── NotificationService.ts
│   │       ├── TemplateEngine.ts
│   │       ├── DeliveryService.ts
│   │       ├── RetryService.ts
│   │       └── PreferenceService.ts
│   ├── domain/
│   │   ├── aggregates/
│   │   │   ├── Notification.ts
│   │   │   ├── NotificationDelivery.ts
│   │   │   ├── NotificationTemplate.ts
│   │   │   └── NotificationPreference.ts
│   │   ├── value-objects/
│   │   ├── services/
│   │   └── events/
│   ├── infrastructure/
│   │   ├── providers/              // Channel-specific implementations
│   │   │   ├── email/
│   │   │   ├── sms/
│   │   │   ├── push/
│   │   │   └── webhook/
│   │   ├── persistence/
│   │   ├── messaging/
│   │   ├── templates/              // Template engine
│   │   └── observability/
│   └── interfaces/
└── config/
```

## Implementation Notes

**CQRS Considerations:**
- Write side: Notification creation, delivery updates
- Read side: Notification status, template queries
- Separate read database for delivery history

**Event-Driven Architecture:**
- Consume payment events
- Publish delivery events
- Dead-letter queue for failed notifications

**Scalability Concerns:**
- High notification volume during peak times
- Rate limiting per channel (SMS especially)
- Template caching for fast rendering
- Queue-based delivery (async processing)

**Security Considerations:**
- PII in notifications (encrypt at rest)
- User preferences must be respected (opt-out)
- Rate limiting to prevent spam
- Template injection prevention (sanitize variables)
- Webhook signature verification

---

# Service 8: Reconciliation Service

## Purpose

**Business Goals and Responsibilities:**
- Reconcile internal records with external providers
- Detect and resolve discrepancies
- Ensure financial data integrity
- Support settlement and chargeback processes
- Generate reconciliation reports

**Problems Solved:**
- Data consistency between systems
- Discrepancy detection and resolution
- Settlement tracking
- Chargeback management
- Compliance and audit requirements

**Why It Should Be Its Own Bounded Context:**
- Reconciliation is a distinct domain (matching, balancing, verifying)
- Complex matching logic and rules
- Independent of payment processing flow
- Requires its own data structures and workflows

## Domain Responsibilities

**Core Business Capabilities:**
- Daily reconciliation with providers
- Discrepancy detection and resolution
- Settlement tracking
- Chargeback processing
- Reconciliation reporting
- Audit trail maintenance

**Invariants and Business Rules:**
- All transactions must be reconciled within SLA
- Discrepancies must be investigated and resolved
- Reconciliation must balance (sum of matches + discrepancies = total)
- Chargebacks must be processed within provider deadlines
- Settlements must be tracked and verified

**Data Ownership Boundaries:**
- **Owns:** Reconciliation records, discrepancy records, settlement records, chargeback records
- **Does Not Own:** Payment transactions (references only), ledger entries (references only)

## Domain Entities

### Entity: ReconciliationRun

**Description:** Represents a single reconciliation execution.

**Key Attributes:**
```typescript
{
  runId: string;
  runType: RunType;               // DAILY, AD_HOC, PROVIDER_SPECIFIC
  providerId: string;
  periodStart: DateTime;
  periodEnd: DateTime;
  status: ReconciliationStatus;  // RUNNING, COMPLETED, FAILED, PARTIALLY_COMPLETED
  summary: {
    totalInternalTransactions: number;
    totalProviderTransactions: number;
    matchedTransactions: number;
    unmatchedInternalTransactions: number;
    unmatchedProviderTransactions: number;
    discrepancies: number;
  };
  startedAt: DateTime;
  completedAt?: DateTime;
  error?: string;
}
```

**Relationships:**
- Has many Discrepancy
- Has many ReconciliationMatch

**Aggregate Root:** Yes

### Entity: Discrepancy

**Description:** Represents a mismatch between internal and provider records.

**Key Attributes:**
```typescript
{
  discrepancyId: string;
  runId: string;
  discrepancyType: DiscrepancyType; // MISSING_IN_INTERNAL, MISSING_IN_PROVIDER, AMOUNT_MISMATCH, STATUS_MISMATCH, TIMING_MISMATCH
  internalTransactionId?: string;
  providerTransactionId?: string;
  internalAmount?: Money;
  providerAmount?: Money;
  difference?: Money;
  status: DiscrepancyStatus;     // OPEN, INVESTIGATING, RESOLVED, IGNORED
  resolution?: string;
  resolvedBy?: string;
  resolvedAt?: DateTime;
  createdAt: DateTime;
  updatedAt: DateTime;
}
```

**Relationships:**
- Belongs to ReconciliationRun aggregate

**Aggregate Root:** No (part of ReconciliationRun)

### Entity: ReconciliationMatch

**Description:** Represents a matched transaction pair.

**Key Attributes:**
```typescript
{
  matchId: string;
  runId: string;
  internalTransactionId: string;
  providerTransactionId: string;
  matchConfidence: number;       // 0-1
  matchedAt: DateTime;
}
```

**Relationships:**
- Belongs to ReconciliationRun aggregate

**Aggregate Root:** No (part of ReconciliationRun)

### Entity: Settlement

**Description:** Represents a settlement batch from a provider.

**Key Attributes:**
```typescript
{
  settlementId: string;
  providerId: string;
  settlementDate: Date;
  settlementBatchId: string;
  totalAmount: Money;
  transactionCount: number;
  feeAmount: Money;
  netAmount: Money;
  currency: string;
  status: SettlementStatus;      // PENDING, RECEIVED, VERIFIED, DISCREPANCY
  verifiedAt?: DateTime;
  discrepancies: string[];
  receivedAt: DateTime;
}
```

**Relationships:**
- Has many SettlementTransaction

**Aggregate Root:** Yes

### Entity: Chargeback

**Description:** Represents a chargeback from a provider.

**Key Attributes:**
```typescript
{
  chargebackId: string;
  providerId: string;
  paymentId: string;
  chargebackAmount: Money;
  chargebackReason: string;
  chargebackDate: Date;
  status: ChargebackStatus;      // PENDING, RESPONDED, WON, LOST, EXPIRED
  responseDeadline: DateTime;
  responseEvidence?: string;     // Documents/evidence for dispute
  respondedAt?: DateTime;
  outcome?: ChargebackOutcome;    // FULL_REVERSAL, PARTIAL_REVERSAL, WON
  createdAt: DateTime;
  updatedAt: DateTime;
}
```

**Relationships:**
- References Payment

**Aggregate Root:** Yes

## Value Objects

| Value Object | Purpose | Properties |
|---|---|---|
| `DiscrepancyType` | Type of mismatch | `type: enum(MISSING_IN_INTERNAL, MISSING_IN_PROVIDER, AMOUNT_MISMATCH, STATUS_MISMATCH)` |
| `DiscrepancyStatus` | Discrepancy resolution status | `status: enum(OPEN, INVESTIGATING, RESOLVED, IGNORED)` |
| `SettlementStatus` | Settlement verification status | `status: enum(PENDING, RECEIVED, VERIFIED, DISCREPANCY)` |
| `ChargebackStatus` | Chargeback processing status | `status: enum(PENDING, RESPONDED, WON, LOST, EXPIRED)` |

## Domain Events

| Event Name | Trigger Conditions | Event Payload |
|---|---|---|
| `ReconciliationStarted` | Reconciliation run initiated | `runId`, `providerId`, `period` |
| `ReconciliationCompleted` | Run completed successfully | `runId`, `summary` |
| `ReconciliationFailed` | Run failed | `runId`, `error`, `partialResults` |
| `DiscrepancyDetected` | New discrepancy found | `discrepancyId`, `type`, `details` |
| `DiscrepancyResolved` | Discrepancy resolved | `discrepancyId`, `resolution`, `resolvedBy` |
| `SettlementReceived` | Provider settlement received | `settlementId`, `providerId`, `amount` |
| `SettlementVerified` | Settlement verified | `settlementId`, `verifiedAt` |
| `ChargebackReceived` | Chargeback initiated by customer | `chargebackId`, `paymentId`, `amount` |
| `ChargebackResponded` | Response submitted | `chargebackId`, `responseEvidence` |
| `ChargebackWon` | Chargeback dispute won | `chargebackId`, `wonAt` |
| `ChargebackLost` | Chargeback dispute lost | `chargebackId`, `lostAt`, `liabilityAmount` |

## External Interfaces

### API Endpoints

| Endpoint | HTTP Method | Request Model | Response Model | Authorization |
|---|---|---|---|---|
| `/api/reconciliation/run` | POST | `RunReconciliationRequest` | `RunReconciliationResponse` | Admin role |
| `/api/reconciliation/runs/{id}` | GET | N/A | `GetRunResponse` | Admin role |
| `/api/reconciliation/discrepancies` | GET | N/A | `ListDiscrepanciesResponse` | Admin role |
| `/api/reconciliation/discrepancies/{id}/resolve` | POST | `ResolveDiscrepancyRequest` | `ResolveDiscrepancyResponse` | Admin role |
| `/api/reconciliation/settlements` | GET | N/A | `ListSettlementsResponse` | Admin role |
| `/api/reconciliation/settlements/{id}/verify` | POST | `VerifySettlementRequest` | `VerifySettlementResponse` | Admin role |
| `/api/reconciliation/chargebacks` | GET | N/A | `ListChargebacksResponse` | Admin role |
| `/api/reconciliation/chargebacks/{id}/respond` | POST | `RespondChargebackRequest` | `RespondChargebackResponse` | Admin role |

### Events Published

| Event Name | When Published | Consumers |
|---|---|---|
| `ReconciliationCompleted` | On successful run | Reporting Service, Ledger Service |
| `DiscrepancyDetected` | On discrepancy found | Alerting Service, Operations Team |
| `SettlementReceived` | On settlement receipt | Ledger Service, Finance Team |
| `ChargebackReceived` | On chargeback initiation | Payment Service, Ledger Service, Alerting Service |

### Events Consumed

| Event Name | Producer | Processing Logic |
|---|---|---|
| `PaymentCompleted` | Payment Service | Include in next reconciliation |
| `ProviderStatementReceived` | Bank Adapter | Trigger reconciliation for provider |

## Integrations

**Upstream Dependencies:**
- Ledger Service (for internal records)
- Bank Adapter Service (for provider statements)

**Downstream Dependencies:**
- Ledger Service (for adjustments)
- Reporting Service
- Alerting Service

**Third-Party Systems:**
- Bank APIs (for statement download)
- Payment gateway APIs (for reports)

## Data Storage

**Database Ownership:**
- Reconciliation runs
- Discrepancy records
- Settlement records
- Chargeback records
- Matching rules

**Tables/Collections:**
- `reconciliation_runs` - Run records
- `discrepancies` - Discrepancy records
- `reconciliation_matches` - Matched transactions
- `settlements` - Settlement records
- `chargebacks` - Chargeback records

**Read and Write Patterns:**
- **Write:** Run records, discrepancies, settlements
- **Read:** Historical reconciliation data, reporting
- **Hot data:** Recent runs and open discrepancies
- **Cold data:** Historical runs (archive after 7 years)

## Service Boundaries

**What Belongs Inside:**
- Reconciliation execution logic
- Discrepancy detection and resolution
- Settlement tracking and verification
- Chargeback processing
- Matching rules and algorithms
- Reconciliation reporting

**What Must Not Belong Inside:**
- Payment processing (belongs in Payment Service)
- Ledger accounting (belongs in Ledger Service)
- Bank API integration (belongs in Bank Adapter)
- Notification delivery (belongs in Notification Service)

**Justification:**
- Reconciliation is a distinct ubiquitous language (matching, balancing, discrepancies)
- Complex algorithms independent of payment flow
- Different temporal characteristics (daily/weekly vs real-time)
- Specialized workflows (investigation, resolution)

## Communication with Other Services

**Synchronous Communication:**
- Ledger Service (internal transaction queries)
- Bank Adapter Service (provider statement queries)

**Asynchronous Communication:**
- Ledger Service (adjustment events)
- Alerting Service (discrepancy alerts)

**Required Contracts:**
- Reconciliation request schema
- Discrepancy schema
- Settlement schema
- Domain event schemas

## Suggested Microservice Structure

```
reconciliation/
├── app/
│   ├── application/
│   │   ├── commands/
│   │   │   ├── RunReconciliationHandler.ts
│   │   │   ├── ResolveDiscrepancyHandler.ts
│   │   │   ├── VerifySettlementHandler.ts
│   │   │   └── RespondChargebackHandler.ts
│   │   ├── queries/
│   │   │   ├── GetRunHandler.ts
│   │   │   ├── ListDiscrepanciesHandler.ts
│   │   │   └── ListChargebacksHandler.ts
│   │   └── services/
│   │       ├── ReconciliationService.ts
│   │       ├── MatchingService.ts
│   │       ├── DiscrepancyService.ts
│   │       ├── SettlementService.ts
│   │       └── ChargebackService.ts
│   ├── domain/
│   │   ├── aggregates/
│   │   │   ├── ReconciliationRun.ts
│   │   │   ├── Discrepancy.ts
│   │   │   ├── Settlement.ts
│   │   │   └── Chargeback.ts
│   │   ├── value-objects/
│   │   ├── services/
│   │   │   ├── ReconciliationDomainService.ts
│   │   │   ├── MatchingAlgorithmService.ts
│   │   │   └── ChargebackDomainService.ts
│   │   └── events/
│   ├── infrastructure/
│   │   ├── persistence/
│   │   ├── messaging/
│   │   ├── algorithms/             // Matching algorithms
│   │   └── observability/
│   └── interfaces/
└── config/
```

## Implementation Notes

**CQRS Considerations:**
- Write side: Reconciliation runs, discrepancies
- Read side: Historical reconciliation data, reporting
- Separate read database for analytics

**Event-Driven Architecture:**
- Publish reconciliation events
- Subscribe to payment events
- Settlement events

**Scalability Concerns:**
- Reconciliation runs can be resource-intensive
- Large transaction volumes require batching
- Matching algorithms must be optimized
- Consider parallel processing for multiple providers

**Security Considerations:**
- Financial data must be encrypted
- Strict access controls
- Audit trail for all reconciliation operations
- Compliance with financial regulations
- Secure storage for chargeback evidence

---

# Context Map and Service Relationships

## 1. Context Map

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        PAYMENT PROCESSING SYSTEM                            │
└─────────────────────────────────────────────────────────────────────────────┘

         ┌──────────────────┐
         │   CLIENT APPS    │
         └─────────┬────────┘
                   │ HTTP/WebSocket
                   ▼
    ┌──────────────────────────────────────────────────────────────┐
    │                    API GATEWAY                                │
    │  (Traffic Management, Auth, Routing, Observability)          │
    └──────────────────────────┬───────────────────────────────────┘
                               │
                               │ HTTP/gRPC
                               ▼
    ┌──────────────────────────────────────────────────────────────┐
    │              PAYMENT PROCESSING SERVICE                      │
    │           (Payment Orchestration & State Machine)           │
    │                                                              │
    │  Responsibilities:                                           │
    │  - Payment lifecycle management                             │
    │  - Coordination of downstream services                      │
    │  - Idempotency enforcement                                   │
    └──────────────────────────┬───────────────────────────────────┘
                               │
           ┌───────────────────┼───────────────────┐
           │                   │                   │
           │ async (events)     │ sync/async         │ async (events)
           │                   │                   │
           ▼                   ▼                   ▼
    ┌──────────────┐  ┌──────────────────┐  ┌──────────────────┐
    │ RISK SERVICE │  │ ROUTER SERVICE   │  │  LEDGER SERVICE   │
    │              │  │                  │  │                  │
    │ - Fraud eval │  │ - Provider sel. │  │ - Double-entry   │
    │ - Rules eng. │  │ - Cost optim.   │  │ - Account mgmt   │
    │ - Risk score │  │ - Circuit break │  │ - Reconciliation │
    └──────┬───────┘  └────────┬─────────┘  └─────────┬────────┘
           │                   │                      │
           │ events            │ decision             │ events
           │                   │                      │
           ▼                   ▼                      ▼
    ┌──────────────┐  ┌──────────────────┐  ┌──────────────────┐
    │   PAYMENT    │  │  BANK ADAPTER    │  │ RECONCILIATION   │
    │   PROCESSING │  │                  │  │                  │
    │   SERVICE    │  │ - Provider API   │  │ - Daily rec.     │
    │              │  │ - Retry logic    │  │ - Discrepancies  │
    │ (consumes    │  │ - Error norm.    │  │ - Settlements    │
    │  risk &      │  │ - Rate limiting  │  │ - Chargebacks    │
    │  routing)    │  └──────────────────┘  └──────────────────┘
    └──────────────┘
           │
           │ events
           │
           ▼
    ┌──────────────────┐
    │ NOTIFICATION     │
    │ SERVICE          │
    │                  │
    │ - Email          │
    │ - SMS            │
    │ - Push           │
    │ - Webhook        │
    └──────────────────┘

LEGEND:
──── Solid line: Synchronous communication (HTTP/gRPC)
 ---- Dashed line: Asynchronous communication (events/messages)
```

## 2. Service Dependency Diagram (Text-Based)

```
DEGREE 1: Infrastructure Layer
├── API Gateway
│   └── No dependencies (stateless)

DEGREE 2: Core Business Services
├── Payment Processing Service
│   ├── Depends on: Risk Service (sync/async)
│   ├── Depends on: Payment Router Service (sync)
│   ├── Depends on: Bank Adapter Service (async, via router)
│   └── Depends on: Ledger Service (async)

DEGREE 3: Supporting Business Services
├── Risk Service
│   ├── Depends on: Merchant Service (sync, for profiles)
│   └── No other service dependencies

├── Payment Router Service
│   ├── Depends on: Bank Adapter Service (sync, for health checks)
│   └── No other service dependencies

├── Bank Adapter Service
│   ├── Depends on: External Providers (HTTP APIs)
│   └── No internal service dependencies

├── Ledger Service
│   ├── Depends on: Payment Service (events consumed)
│   └── No other service dependencies

DEGREE 4: Async-Dependent Services
├── Notification Service
│   ├── Consumes events from: Payment Service
│   ├── Consumes events from: Ledger Service
│   ├── Consumes events from: Risk Service
│   └── Depends on: External Providers (email, SMS, push)

├── Reconciliation Service
│   ├── Consumes events from: Payment Service
│   ├── Depends on: Ledger Service (sync, for data access)
│   ├── Depends on: Bank Adapter Service (sync, for statements)
│   └── Depends on: External Providers (for settlement reports)

DEGREE 5: Cross-Cutting Services (Not explicitly in article but implied)
├── Observability Platform
│   ├── Receives traces from: All services
│   ├── Receives metrics from: All services
│   └── Receives logs from: All services
```

## 3. Recommended Deployment Order

**Phase 1: Foundation (Week 1-2)**
1. **API Gateway** - Must be deployed first to provide entry point
2. **Payment Processing Service** - Core orchestration (minimal version without downstream)

**Phase 2: Core Payment Flow (Week 3-4)**
3. **Risk Service** - Required for payment processing
4. **Payment Router Service** - Required for provider selection
5. **Bank Adapter Service** - Required for payment execution

**Phase 3: Financial Integrity (Week 5-6)**
6. **Ledger Service** - Critical for accounting and reconciliation

**Phase 4: Customer Experience (Week 7)**
7. **Notification Service** - Important for customer communication

**Phase 5: Operations & Compliance (Week 8)**
8. **Reconciliation Service** - Critical for financial integrity

**Rationale:**
- Foundation services first (gateway, payment processing)
- Core payment flow before supporting services
- Financial integrity before customer-facing features
- Reconciliation last (depends on ledger and payment data)

## 4. Risks and Boundary Conflicts

### Identified Risks

| Risk | Severity | Impact | Mitigation |
|---|---|---|---|
| **Payment Service ↔ Risk Service boundary** | Medium | Risk evaluation logic could bleed into payment service | Strict interface contracts, separate teams |
| **Router Service ↔ Bank Adapter Service boundary** | Medium | Router may become tightly coupled to specific provider implementations | Provider abstraction layer, protocol-agnostic routing |
| **Ledger Service ↔ Reconciliation Service boundary** | Low | Both services access similar financial data | Clear ownership: Ledger owns records, Reconciliation owns matching |
| **Payment Service ↔ Ledger Service boundary** | Low | Ledger may become dependent on Payment Service internals | Event-based communication only, no direct coupling |
| **Notification Service timing** | Medium | Async notifications may arrive before payment completion | Order guarantees, notification scheduling |
| **Reconciliation Service data access** | High | Requires access to both internal and external data | Secure access patterns, data masking |
| **Bank Adapter Service provider fragmentation** | High | Too many provider-specific implementations in one service | Provider abstraction layer, separate provider modules |

### Boundary Conflicts and Resolution

**Conflict 1: Idempotency Responsibility**
- **Question:** Should Payment Service or API Gateway handle idempotency?
- **Resolution:** Payment Service owns idempotency (business logic), Gateway only handles transport-level concerns
- **Justification:** Idempotency is a business invariant, not infrastructure

**Conflict 2: Retry Logic Ownership**
- **Question:** Should retry logic live in Payment Service, Router Service, or Bank Adapter Service?
- **Resolution:** Each service handles its own retry logic
  - Payment Service: Workflow-level retries (entire payment)
  - Router Service: Provider selection retries (choose different provider)
  - Bank Adapter Service: Transient failure retries (same provider)
- **Justification:** Different retry strategies at different layers

**Conflict 3: Routing Decision Ownership**
- **Question:** Should Payment Service or Router Service decide routing?
- **Resolution:** Router Service owns routing logic, Payment Service only invokes it
- **Justification:** Routing is complex and should be isolated

**Conflict 4: Ledger Entry Creation Timing**
- **Question:** When should ledger entries be created? At payment creation or completion?
- **Resolution:** Two-phase approach
  - Pending entry at payment creation (reserves funds)
  - Finalized entry at payment completion (actual transaction)
- **Justification:** Matches accounting practice (commitments vs actuals)

**Conflict 5: Failure Notification Ownership**
- **Question:** Should Payment Service or Risk Service send fraud rejection notifications?
- **Resolution:** Notification Service owns all notifications, triggered by events from either service
- **Justification:** Single notification channel, clear separation of concerns

### Alternative Boundary Options

**Alternative 1: Merge Router and Bank Adapter**
- **Proposal:** Combine routing and provider integration into one service
- **Pros:** Fewer services, less latency
- **Cons:** Violates SRP, routing logic coupled with integration
- **Recommendation:** Keep separate (as designed)

**Alternative 2: Split Risk Service into Rules and Evaluation**
- **Proposal:** Separate rule engine from evaluation execution
- **Pros:** Clear separation of concerns
- **Cons:** Added latency, unnecessary complexity
- **Recommendation:** Keep together (as designed)

**Alternative 3: Separate Notification Service by Channel**
- **Proposal:** Email Service, SMS Service, Push Service
- **Pros:** Channel-specific optimization
- **Cons:** Too many services, duplicated logic
- **Recommendation:** Keep unified (as designed)

## 5. Recommended Ownership of Entities and Business Processes

### Entity Ownership Matrix

| Entity | Owning Service | Justification |
|---|---|---|
| `Payment` (aggregate) | Payment Processing Service | Core payment lifecycle |
| `PaymentAttempt` | Payment Processing Service | Part of payment aggregate |
| `RiskEvaluation` | Risk Service | Risk evaluation is distinct domain |
| `RiskRule` | Risk Service | Risk configuration is risk domain |
| `MerchantRiskProfile` | Risk Service | Merchant risk is risk concern |
| `PaymentProvider` | Payment Router Service | Provider management is routing concern |
| `RoutingRule` | Payment Router Service | Routing logic is router domain |
| `RoutingDecision` | Payment Router Service | Routing decision is router domain |
| `ProviderIntegration` | Bank Adapter Service | Integration is adapter concern |
| `ProviderRequestLog` | Bank Adapter Service | Request log belongs to integration |
| `Account` | Ledger Service | Account is accounting domain |
| `JournalEntry` | Ledger Service | Journal is accounting domain |
| `JournalEntryLine` | Ledger Service | Part of journal entry aggregate |
| `Reconciliation` | Reconciliation Service | Reconciliation is distinct domain |
| `Discrepancy` | Reconciliation Service | Part of reconciliation aggregate |
| `Settlement` | Reconciliation Service | Settlement is reconciliation concern |
| `Chargeback` | Reconciliation Service | Chargeback is reconciliation concern |
| `Notification` | Notification Service | Notification is distinct domain |
| `NotificationDelivery` | Notification Service | Part of notification aggregate |
| `NotificationTemplate` | Notification Service | Template is notification concern |
| `NotificationPreference` | Notification Service | Preference is notification concern |

### Business Process Ownership

| Business Process | Owning Service | Participating Services | Process Flow |
|---|---|---|---|
| **Payment Creation** | Payment Processing | API Gateway, Risk Service, Router Service | Gateway → Payment → Risk → Router → Payment |
| **Payment Authorization** | Payment Processing | Risk Service, Router Service, Bank Adapter | Payment orchestrates downstream services |
| **Risk Evaluation** | Risk Service | Payment Service (consumer) | Payment triggers, Risk evaluates, Payment consumes |
| **Payment Routing** | Router Service | Payment Service (consumer), Bank Adapter | Payment requests, Router selects, Adapter executes |
| **Bank Integration** | Bank Adapter | Router Service (consumer) | Router requests, Adapter integrates with provider |
| **Ledger Posting** | Ledger Service | Payment Service (trigger) | Payment completes → Ledger posts |
| **Reconciliation** | Reconciliation Service | Ledger Service, Bank Adapter | Reconciliation queries both, identifies discrepancies |
| **Notification Delivery** | Notification Service | All services (producers) | Services publish events → Notification delivers |
| **Settlement Processing** | Reconciliation Service | Ledger Service, Bank Adapter | Settlement received → Ledger update → Reconciliation verify |
| **Chargeback Processing** | Reconciliation Service | Payment Service, Ledger Service | Chargeback received → Ledger update → Payment update |

### Cross-Cutting Concerns Ownership

| Concern | Owning Service/Area | Justification |
|---|---|---|
| **Observability (tracing, metrics, logs)** | All Services (central platform) | Every service emits, Observability Platform collects |
| **Authentication/Authorization** | API Gateway | Centralized auth at entry point |
| **Rate Limiting** | API Gateway | Centralized at gateway |
| **Idempotency** | Payment Processing Service | Business invariant, not infrastructure |
| **Circuit Breaking** | Router Service | Provider-specific circuit breaking |
| **Retry Logic** | All Services (layer-specific) | Each service handles its own retry strategy |
| **Data Encryption** | Infrastructure (all services) | Cross-cutting security concern |
| **Audit Logging** | Ledger Service, Reconciliation Service | Financial services require audit trails |
| **Template Management** | Notification Service | Notification-specific concern |
| **Health Checking** | Router Service, Bank Adapter | Provider health is routing concern |

### Data Access Patterns

| Service | Read Access | Write Access | External Data Access |
|---|---|---|---|
| **API Gateway** | None (stateless) | None (stateless) | Merchant Service (auth validation) |
| **Payment Processing** | Own database only | Own database only | Read-only: Merchant profiles, Risk decisions, Routing decisions |
| **Risk Service** | Own database only | Own database only | Read-only: Payment details, Merchant profiles |
| **Router Service** | Own database only | Own database only | Read-only: Payment details, Bank Adapter health |
| ** Bank Adapter** | Own database only | Own database only | External provider APIs (write) |
| **Ledger Service** | Own database only | Own database only | Read-only: Payment details (for reference) |
| **Notification Service** | Own database only | Own database only | Read-only: Payment details, Customer preferences |
| **Reconciliation Service** | Own database only | Own database only | Read-only: Ledger data, Bank Adapter provider data |

---

# Summary

This analysis identifies **8 distinct bounded contexts** in the distributed payment system:

1. **API Gateway** - Infrastructure for traffic management
2. **Payment Processing Service** - Core payment orchestration
3. **Risk Service** - Fraud evaluation and risk assessment
4. **Payment Router Service** - Provider selection and routing
5. **Bank Adapter Service** - External provider integration
6. **Ledger Service** - Financial accounting and double-entry bookkeeping
7. **Notification Service** - Multi-channel customer communication
8. **Reconciliation Service** - Financial reconciliation and discrepancy management

Each service has:
- Clear domain responsibilities
- Distinct ubiquitous language
- Independent data ownership
- Well-defined boundaries
- Explicit communication patterns

The design emphasizes:
- **Separation of concerns** - Each service owns a distinct domain
- **Event-driven communication** - Async events for loose coupling
- **Domain-driven boundaries** - Based on ubiquitous language and invariants
- **Scalability** - Services can scale independently
- **Resilience** - Failure isolation and circuit breaking

The boundaries are designed to support the observability needs described in the article while maintaining clean domain separation and enabling independent evolution of each bounded context.
