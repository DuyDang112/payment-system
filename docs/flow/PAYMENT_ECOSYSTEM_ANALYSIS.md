# Complete Payment Ecosystem Analysis

## Executive Summary

This payment system consists of **4 microservices** that work together to process payments from end-to-end:

1. **PaymentProcessing** - Payment lifecycle orchestrator and state machine
2. **PaymentRouter** - Provider selection and routing logic
3. **RiskAssessment** - Fraud detection and risk evaluation
4. **BankAdapter** - Payment provider integration abstraction

---

## Service 1: PaymentProcessing

### **Business Purpose & Responsibilities**
PaymentProcessing is the **orchestrator** of the payment ecosystem. It manages the complete payment lifecycle from creation to completion, acting as the central coordination point for all payment operations.

**Core Responsibilities:**
- Payment creation and idempotency management
- Payment state transitions and lifecycle management
- Coordination of payment workflows across services
- Payment attempt tracking and retry logic
- Domain event publishing for service integration

### **Key Entities & Data**

#### **Payment (Aggregate Root)**
```csharp
public sealed class Payment
{
    public Guid Id { get; private set; }
    public string PaymentId { get; private set; }        // External payment ID: "pay_abc123"
    public string MerchantId { get; private set; }
    public string CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public string? PaymentMethodToken { get; private set; }
    public int RetryCount { get; private set; }
    
    // Navigation properties
    public IReadOnlyCollection<StateTransition> StateTransitions { get; }
    public IReadOnlyCollection<PaymentAttempt> Attempts { get; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
}
```

#### **PaymentStatus (State Machine)**
```csharp
public enum PaymentStatus
{
    Created,           // Initial state after payment creation
    Processing,        // Payment is being processed
    RiskEvaluation,    // Risk assessment in progress
    Routing,           // Provider routing in progress
    Authorizing,       // Authorization with provider in progress
    Completed,         // Payment successfully completed
    Failed,            // Payment failed (will retry)
    Cancelled          // Payment cancelled by user/merchant
}
```

#### **PaymentAttempt (Retry Tracking)**
```csharp
public sealed class PaymentAttempt
{
    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string Provider { get; private set; }         // Which provider was used
    public AttemptStatus Status { get; private set; }    // Success/Failed/InProgress
    public string? FailureReason { get; private set; }
    public DateTime AttemptedAt { get; private set; }
}

public enum AttemptStatus
{
    InProgress,
    Success,
    Failed,
    Timeout
}
```

### **Public APIs & Functions**

#### **POST /api/payments** - Create Payment
```json
// Request
{
  "merchantId": "merchant_001",
  "customerId": "customer_abc123",
  "amount": 100.00,
  "currency": "USD",
  "idempotencyKey": "unique-key-12345",
  "paymentMethodToken": "pm_token_xyz",
  "metadata": {
    "orderId": "order_123",
    "productId": "product_456"
  }
}

// Response
{
  "paymentId": "pay_abc123xyz",
  "status": "Created",
  "amount": 100.00,
  "currency": "USD",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

#### **GET /api/payments/{paymentId}** - Get Payment Status
```json
// Response
{
  "paymentId": "pay_abc123xyz",
  "status": "Authorizing",
  "amount": 100.00,
  "currency": "USD",
  "attempts": [
    {
      "attemptNumber": 1,
      "provider": "stripe",
      "status": "InProgress",
      "attemptedAt": "2024-01-15T10:31:00Z"
    }
  ],
  "stateTransitions": [
    {
      "fromStatus": "Created",
      "toStatus": "Processing",
      "transitionedAt": "2024-01-15T10:30:30Z"
    }
  ]
}
```

#### **DELETE /api/payments/{paymentId}** - Cancel Payment
```json
// Response
{
  "paymentId": "pay_abc123xyz",
  "status": "Cancelled",
  "cancelledAt": "2024-01-15T10:32:00Z"
}
```

### **Interactions with Other Services**

**As Integration Event Publisher:**
- Publishes `PaymentCreatedEvent` → Triggers routing & risk evaluation
- Publishes `PaymentStatusChangedEvent` → Notifies other services of state changes
- Publishes `PaymentCompletedEvent` → Triggers settlement, notifications
- Publishes `PaymentFailedEvent` → Triggers retry logic, customer notifications

**As Service Consumer:**
- Currently **does not directly call** other services
- Other services subscribe to PaymentProcessing events via message bus
- Future enhancement: Direct service calls for synchronous workflows

### **Business Rules & Decision-Making**

#### **Idempotency Management**
- Each payment request must include unique `idempotencyKey`
- If duplicate key received within 24 hours → Return existing payment
- If expired key (> 24 hours) → Return error, request new key
- Prevents duplicate payment processing

#### **State Transition Validation**
```csharp
// Only valid transitions allowed:
Created → Processing → RiskEvaluation → Routing → Authorizing → Completed
                                                  ↓               ↓
                                             Failed ←---------→ Cancelled

// Invalid transitions (throw exceptions):
Completed → Failed (cannot fail completed payment)
Failed → Completed (must retry via new attempt)
```

#### **Retry Logic**
- Tracks `RetryCount` on payment
- Each failed attempt increments counter
- Max 3 retry attempts before permanent failure
- Each retry uses same payment ID with new attempt number

#### **Cancellation Rules**
- Can cancel if status is: Created, Processing, RiskEvaluation, Routing, Authorizing
- Cannot cancel if: Completed, Failed
- Failed payments require new payment creation

### **Position in Payment Lifecycle**

```
PaymentProcessing is the CENTRAL ORCHESTRATOR:

1. Payment Created → State: Created
2. Begin Processing → State: Processing  
3. Publish Events → Other services react
4. Monitor State → Track progress through lifecycle
5. Handle Failures → Coordinate retries and recovery
6. Final State → Completed/Failed/Cancelled
```

---

## Service 2: PaymentRouter

### **Business Purpose & Responsibilities**
PaymentRouter is the **provider selection engine** that determines which payment provider should process each payment based on business rules, costs, performance, and availability.

**Core Responsibilities:**
- Provider selection based on routing strategies
- Circuit breaker pattern for provider health
- Cost optimization and provider performance tracking
- Fallback provider management

### **Key Entities & Data**

#### **PaymentProvider**
```csharp
public sealed class PaymentProvider
{
    public string ProviderId { get; private set; }           // "stripe_prod_001"
    public string ProviderName { get; private set; }         // "Stripe Production"
    public ProviderType ProviderType { get; private set; }    // Stripe, Adyen, Bank, etc.
    public string[] SupportedCurrencies { get; private set; } // ["USD", "EUR", "GBP"]
    public PaymentMethod[] SupportedMethods { get; private set; } // [Card, BankTransfer]
    public int Priority { get; private set; }                // 1 (highest) to 10 (lowest)
    public bool IsEnabled { get; private set; }
    public decimal MinAmount { get; private set; }          // $0.01 minimum
    public decimal MaxAmount { get; private set; }          // $1,000,000 maximum
    public CostConfiguration CostConfig { get; private set; } // Fees and pricing
    public PerformanceMetrics Metrics { get; private set; }   // Success rates, latency
    public HealthStatus HealthStatus { get; private set; }   // HEALTHY, DEGRADED, UNHEALTHY
    public CircuitState CircuitBreakerState { get; private set; } // CLOSED, OPEN, HALF_OPEN
}

public enum ProviderType { Stripe, Adyen, Bank, Wallet }
public enum PaymentMethod { CreditCard, DebitCard, BankTransfer, DigitalWallet }
public enum HealthStatus { HEALTHY, DEGRADED, UNHEALTHY }
public enum CircuitState { CLOSED (working), OPEN (failed), HALF_OPEN (testing) }
```

#### **RoutingDecision**
```csharp
public sealed class RoutingDecision
{
    public string DecisionId { get; private set; }
    public string PaymentId { get; private set; }
    public string MerchantId { get; private set; }
    public string SelectedProviderId { get; private set; }     // "stripe_prod_001"
    public string[] AlternativeProviderIds { get; private set; } // ["adyen_prod_001", "bank_001"]
    public RoutingStrategy Strategy { get; private set; }      // COST_OPTIMIZED, PERFORMANCE, etc.
    public string DecisionReason { get; private set; }          // "Lowest cost provider"
    public Money CostEstimate { get; private set; }             // Estimated fees
    public DateTime DecisionMadeAt { get; private set; }
}

public enum RoutingStrategy
{
    LowestCost,           // Select provider with lowest fees
    HighestSuccessRate,   // Select provider with best success rate  
    LowestLatency,        // Select fastest provider
    PriorityBased,        // Use priority order configuration
    RoundRobin,           // Distribute load evenly
    WeightedRandom        // Weight by performance metrics
}
```

#### **CostConfiguration**
```csharp
public sealed record CostConfiguration
{
    public Money FixedFee { get; init; }           // $0.30 per transaction
    public decimal PercentageRate { get; init; }  // 2.9% of transaction amount
    public int Priority { get; init; }            // Provider priority
}
```

### **Public APIs & Functions**

#### **POST /api/router/route** - Route Payment
```json
// Request
{
  "paymentId": "pay_abc123",
  "merchantId": "merchant_001",
  "amount": 100.00,
  "currency": "USD",
  "paymentMethod": "CreditCard",
  "countryCode": "US",
  "strategy": "LowestCost"
}

// Response
{
  "decisionId": "route_xyz789",
  "paymentId": "pay_abc123",
  "selectedProviderId": "stripe_prod_001",
  "providerName": "Stripe Production",
  "alternativeProviderIds": ["adyen_prod_001", "bank_001"],
  "strategy": "LowestCost",
  "decisionReason": "Selected lowest cost provider: $3.20 vs $3.50",
  "costEstimate": {
    "amount": 3.20,
    "currency": "USD"
  },
  "decisionMadeAt": "2024-01-15T10:30:35Z"
}
```

#### **Provider Management APIs**
- **GET /api/router/providers** - List all providers
- **POST /api/router/providers** - Add new provider
- **PUT /api/router/providers/{id}** - Update provider configuration
- **DELETE /api/router/providers/{id}** - Disable provider

### **Interactions with Other Services**

**As Event Consumer:**
- Subscribes to `PaymentCreatedEvent` → Triggers routing decision
- Subscribes to `PaymentCompletedEvent` → Updates provider performance metrics
- Subscribes to `PaymentFailedEvent` → Updates success rates, may open circuit breaker

**As Event Publisher:**
- Publishes `PaymentRouteSelectedEvent` → BankAdapter uses selected provider
- Publishes `ProviderHealthChangedEvent` → Other services avoid unhealthy providers
- Publishes `ProviderCircuitOpenedEvent` → Emergency routing changes

**External Provider Interactions:**
- **Health Check API** → Pings provider endpoints: `/health`
- **Performance Monitoring** → Tracks latency, success rates, error rates
- **Circuit Breaker** → Automatically opens provider on repeated failures

### **Business Rules & Decision-Making**

#### **Provider Eligibility Rules**
```csharp
// Provider must meet ALL criteria:
provider.IsEnabled                          // Provider is active
provider.SupportsCurrency(request.Currency) // Handles USD/EUR/etc.
provider.SupportsMethod(request.Method)     // Handles Card/Bank/etc.
provider.IsWithinLimits(request.Amount)    // Amount within min/max
circuitBreaker.IsProviderAvailable(id)     // Circuit breaker not OPEN
```

#### **Routing Strategies**

**1. LowestCost Strategy**
```
Calculate total fees for each eligible provider:
total_cost = fixed_fee + (amount × percentage_rate)
Select provider with minimum total_cost
```

**2. HighestSuccessRate Strategy**
```
Get success rate from provider metrics:
success_rate = successful_transactions / total_transactions
Select provider with highest success_rate (> 95% threshold)
```

**3. Priority-Based Strategy**
```
Sort eligible providers by Priority (1 = highest)
Select first provider that is healthy
Use as fallback mechanism
```

#### **Circuit Breaker Logic**
```
Provider Circuit States:
CLOSED (Normal) → Provider is working normally
OPEN (Failed) → Provider has failed 5+ times, reject all requests
HALF_OPEN (Testing) → Allow 1 test request to check if recovered

Transition Rules:
CLOSED → OPEN: After 5 consecutive failures
OPEN → HALF_OPEN: After 30 seconds cooling period  
HALF_OPEN → CLOSED: Test request succeeds
HALF_OPEN → OPEN: Test request fails
```

### **Position in Payment Lifecycle**

```
PaymentRouter acts AFTER payment creation:

1. Payment Created → PaymentProcessing publishes event
2. Router Receives Event → Analyzes payment requirements
3. Provider Selection → Applies routing strategy
4. Decision Made → Publishes selected provider
5. BankAdapter Uses Decision → Calls authorize with selected provider
```

---

## Service 3: RiskAssessment

### **Business Purpose & Responsibilities**
RiskAssessment is the **fraud detection and risk evaluation engine** that analyzes payment transactions for fraud risk, velocity violations, and policy compliance.

**Core Responsibilities:**
- Real-time fraud risk scoring
- Velocity limit enforcement (transaction frequency limits)
- Whitelist/blacklist management
- Merchant risk profile enforcement
- Rule-based fraud detection

### **Key Entities & Data**

#### **RiskEvaluation**
```csharp
public sealed class RiskEvaluation
{
    public Guid Id { get; private set; }
    public string EvaluationId { get; private set; }        // "risk_eval_123"
    public string PaymentId { get; private set; }
    public string MerchantId { get; private set; }
    public string CustomerId { get; private set; }
    public int RiskScore { get; private set; }               // 0-100 (0 = safe, 100 = risky)
    public string Decision { get; private set; }             // "APPROVE", "REJECT", "REVIEW"
    public DateTime EvaluatedAt { get; private set; }
    public decimal? Amount { get; private set; }
    public string? Currency { get; private set; }
    public string? CountryCode { get; private set; }
    public string? IpAddress { get; private set; }
    public string? CustomerEmail { get; private set; }
    public IReadOnlyCollection<RuleTrigger> TriggeredRules { get; }
}

public enum RiskDecision { APPROVE, REJECT, REVIEW }
```

#### **RiskRule**
```csharp
public sealed class RiskRule
{
    public string RuleId { get; private set; }
    public string RuleName { get; private set; }             // "High Amount Check"
    public RuleType RuleType { get; private set; }           // VELOCITY, AMOUNT, GEO, etc.
    public RuleAction Action { get; private set; }           // BLOCK, WARN, SCORE_ONLY
    public int ScoreImpact { get; private set; }             // How much it adds to risk score
    public string ConditionJson { get; private set; }        // Rule conditions
    public bool IsActive { get; private set; }
}

public enum RuleType 
{ 
    VelocityCheck,        // Transaction frequency limits
    AmountThreshold,      // Amount too high/low
    GeographicRisk,       // High-risk countries
    EmailRisk,            // Suspicious email patterns
    IpRisk,              // Suspicious IP addresses
    DeviceFingerprint,   // Device reputation
    BlacklistMatch,      // Entity blacklisted
    WhitelistMatch       // Entity whitelisted
}
```

#### **MerchantRiskProfile**
```csharp
public sealed class MerchantRiskProfile
{
    public string MerchantId { get; private set; }
    public int AutoRejectThreshold { get; private set; }    // Score >= 80 → Auto reject
    public int ManualReviewThreshold { get; private set; }   // Score >= 50 → Manual review
    public List<VelocityLimit> VelocityLimits { get; private set; }
    public bool StrictMode { get; private set; }              // Block on any rule trigger
}

public sealed record VelocityLimit(
    VelocityWindow WindowType,    // PER_MINUTE, PER_HOUR, PER_DAY
    int Limit                      // Max transactions allowed
);
```

#### **WhitelistEntry & BlacklistEntry**
```csharp
public sealed class WhitelistEntry
{
    public string EntityType { get; private set; }            // "Customer", "IP", "Email"
    public string EntityValue { get; private set; }          // "customer_123", "192.168.1.1"
    public string Reason { get; private set; }                // "VIP customer, auto-approve"
}

public sealed class BlacklistEntry
{
    public string EntityType { get; private set; }            // "Customer", "IP", "Email"  
    public string EntityValue { get; private set; }          // "fraud_customer_456"
    public string Reason { get; private set; }                // "Previous chargebacks"
}
```

### **Public APIs & Functions**

#### **POST /api/risk/evaluate** - Evaluate Risk
```json
// Request
{
  "paymentId": "pay_abc123",
  "merchantId": "merchant_001",
  "customerId": "customer_xyz789",
  "amount": 1000.00,
  "currency": "USD",
  "countryCode": "US",
  "ipAddress": "192.168.1.100",
  "customerEmail": "customer@example.com"
}

// Response - APPROVED
{
  "evaluationId": "risk_eval_456",
  "paymentId": "pay_abc123",
  "riskScore": 15,
  "decision": "APPROVE",
  "evaluatedAt": "2024-01-15T10:30:40Z",
  "triggeredRules": [
    {
      "ruleId": "amount_threshold",
      "ruleName": "Amount Check",
      "ruleType": "AmountThreshold",
      "action": "SCORE_ONLY",
      "scoreImpact": 15,
      "description": "High transaction amount (> $500)"
    }
  ]
}

// Response - REJECTED  
{
  "evaluationId": "risk_eval_789",
  "paymentId": "pay_abc123",
  "riskScore": 85,
  "decision": "REJECT",
  "evaluatedAt": "2024-01-15T10:30:40Z",
  "triggeredRules": [
    {
      "ruleId": "velocity_exceeded",
      "ruleName": "Velocity Limit Check",
      "ruleType": "VelocityCheck",
      "action": "BLOCK",
      "scoreImpact": 50,
      "description": "Customer exceeded 5 transactions per hour limit"
    },
    {
      "ruleId": "high_risk_country",
      "ruleName": "High-Risk Country", 
      "ruleType": "GeographicRisk",
      "action": "BLOCK",
      "scoreImpact": 35,
      "description": "Transaction from high-risk country"
    }
  ]
}
```

#### **Management APIs**
- **GET /api/risk/rules** - List all risk rules
- **POST /api/risk/rules** - Create new risk rule
- **PUT /api/risk/rules/{id}** - Update risk rule
- **POST /api/risk/whitelist** - Add entity to whitelist
- **POST /api/risk/blacklist** - Add entity to blacklist

### **Interactions with Other Services**

**As Event Consumer:**
- Subscribes to `PaymentCreatedEvent` → Triggers risk evaluation
- Subscribes to `PaymentFailedEvent` → May blacklist customer if repeated failures

**As Event Publisher:**
- Publishes `RiskEvaluationCompletedEvent` → PaymentProcessing continues workflow
- Publishes `SuspiciousActivityDetectedEvent` → Security team notification
- Publishes `RiskEvaluationFailedEvent` → Error handling

### **Business Rules & Decision-Making**

#### **Risk Evaluation Process**

```
1. WHITELIST CHECK (Highest Priority)
   If customer in whitelist → APPROVE immediately (score = 0)
   
2. BLACKLIST CHECK (Block Immediately)  
   If customer/IP/email in blacklist → REJECT immediately (score = 100)
   
3. VELOCITY LIMITS ENFORCEMENT
   Check transaction frequency:
   - PER_MINUTE: Max 3 transactions per minute
   - PER_HOUR: Max 10 transactions per hour  
   - PER_DAY: Max 50 transactions per day
   
4. RULE ENGINE EVALUATION
   For each active rule:
   - Evaluate rule conditions (amount, country, IP, etc.)
   - If rule triggered → Add ScoreImpact to total
   - Collect rule triggers for response
   
5. DECISION MAKING
   final_score = sum of all triggered rule scores
   
   if (score >= AutoRejectThreshold) → REJECT
   if (score >= ManualReviewThreshold) → REVIEW (requires manual approval)
   if (score < ManualReviewThreshold) → APPROVE
```

#### **Example Rule Engine Rules**

```csharp
// Rule 1: Amount Threshold Check
if (amount > 1000) {
    AddScoreImpact(25);  // High amount = somewhat risky
}

// Rule 2: Geographic Risk Check  
highRiskCountries = ["XX", "YY", "ZZ"]; // Problematic countries
if (highRiskCountries.Contains(countryCode)) {
    AddScoreImpact(40);  // High risk country = very risky
    BlockImmediately();  // Block regardless of score
}

// Rule 3: Email Risk Check
if (email.Contains("+") || email.Contains("..")) {
    AddScoreImpact(15);  // Suspicious email pattern
}

// Rule 4: IP Address Risk Check
if (IsTorExitNode(ipAddress) || IsKnownProxy(ipAddress)) {
    AddScoreImpact(30);  // Anonymous proxy = risky
}

// Rule 5: Velocity Check  
transactions_last_hour = GetTransactionCount(customerId, hours: 1);
if (transactions_last_hour > 10) {
    AddScoreImpact(50);  // Excessive velocity = very risky
}
```

### **Position in Payment Lifecycle**

```
RiskAssessment acts AFTER payment creation, BEFORE routing:

1. Payment Created → PaymentProcessing publishes event
2. Risk Service Receives → Starts risk evaluation  
3. Whitelist Check → If whitelisted, approve immediately
4. Blacklist Check → If blacklisted, reject immediately
5. Velocity Checks → Ensure customer within limits
6. Rule Engine → Score transaction risk
7. Decision Made → APPROVE/REJECT/REVIEW
8. Event Published → PaymentProcessing continues/rejects payment
```

---

## Service 4: BankAdapter

### **Business Purpose & Responsibilities**
BankAdapter is the **payment provider integration abstraction layer** that normalizes communication with external payment providers (Stripe, Adyen, banks, etc.) and handles technical concerns like retries, rate limiting, and error normalization.

**Core Responsibilities:**
- Provider API integration and protocol handling
- Retry logic with exponential backoff
- Rate limiting per provider
- Request/response logging with PCI-DSS compliance
- Error normalization across providers
- Provider health monitoring

### **Key Entities & Data**

#### **Provider (Configuration Entity)**
```csharp
public sealed class Provider
{
    public string IntegrationId { get; private set; }        // Internal GUID
    public string ProviderId { get; private set; }           // "stripe_prod_001"
    public ProviderType ProviderType { get; private set; }   // Stripe, Adyen, Bank, Wallet
    public string ApiVersion { get; private set; }          // "v1", "2023-01-01"
    public string Endpoint { get; private set; }            // "https://api.stripe.com"
    public AuthConfig AuthConfig { get; private set; }      // API keys, OAuth tokens
    public RateLimits RateLimits { get; private set; }      // Max requests per second
    public RetryConfig RetryConfig { get; private set; }     // Retry policies
    public Timeouts Timeouts { get; private set; }           // Connection/read timeouts
    public HashSet<Operation> SupportedOperations { get; private set; }
    public bool IsActive { get; private set; }
}

public enum ProviderType { Stripe, Adyen, Bank, Wallet }
public enum Operation { Authorize, Capture, Refund, Void }
```

#### **ProviderRequestLog (Audit Trail)**
```csharp
public sealed class ProviderRequestLog
{
    public string LogId { get; private set; }
    public string PaymentId { get; private set; }
    public string ProviderId { get; private set; }
    public Operation Operation { get; private set; }         // Authorize, Capture, etc.
    public DateTime RequestSentAt { get; private set; }
    public DateTime? ResponseReceivedAt { get; private set; }
    public int? Duration { get; private set; }               // Response time in ms
    public string RequestPayload { get; private set; }       // Sanitized request (PCI-DSS)
    public string ResponsePayload { get; private set; }      // Sanitized response (PCI-DSS)
    public int? HttpStatusCode { get; private set; }         // 200, 400, 500, etc.
    public ProviderResult Result { get; private set; }       // Success, RetryableError, FatalError
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
}

public enum ProviderResult { Success, RetryableError, FatalError }
```

### **Public APIs & Functions**

#### **POST /api/bank/authorize** - Authorize Payment
```json
// Request
{
  "paymentId": "pay_abc123",
  "providerId": "stripe_prod_001",
  "amount": 100.00,
  "currency": "USD",
  "metadata": {
    "orderId": "order_123",
    "customerId": "customer_xyz789"
  }
}

// Response - Success
{
  "paymentId": "pay_abc123",
  "providerId": "stripe_prod_001",
  "providerTransactionId": "pi_3abcdef123456",
  "success": true,
  "metadata": {
    "status": "requires_capture",
    "clientSecret": "pi_3abcdef123456_secret_xyz789"
  }
}

// Response - Failure
{
  "paymentId": "pay_abc123", 
  "providerId": "stripe_prod_001",
  "success": false,
  "errorCode": "card_declined",
  "errorMessage": "Your card was declined.",
  "metadata": {}
}
```

#### **POST /api/bank/capture** - Capture Authorized Payment
```json
// Request  
{
  "paymentId": "pay_abc123",
  "providerId": "stripe_prod_001",
  "authorizationToken": "pi_3abcdef123456",
  "amount": 100.00,
  "currency": "USD",
  "metadata": {
    "captureReason": "Order fulfilled"
  }
}

// Response
{
  "paymentId": "pay_abc123",
  "providerId": "stripe_prod_001", 
  "providerTransactionId": "ch_3captured_xyz789",
  "success": true,
  "metadata": {
    "status": "succeeded",
    "capturedAt": "2024-01-15T10:35:00Z"
  }
}
```

#### **POST /api/bank/refund** - Refund Payment
```json
// Request
{
  "paymentId": "pay_abc123",
  "providerId": "stripe_prod_001",
  "authorizationToken": "ch_3captured_xyz789",
  "amount": 50.00,
  "currency": "USD",
  "metadata": {
    "reason": "Customer request"
  }
}

// Response
{
  "paymentId": "pay_abc123",
  "providerId": "stripe_prod_001",
  "providerTransactionId": "re_3refunded_xyz789",
  "success": true,
  "metadata": {
    "status": "succeeded",
    "refundAmount": 50.00
  }
}
```

#### **POST /api/bank/void** - Void Authorization
```json
// Request
{
  "paymentId": "pay_abc123",
  "providerId": "stripe_prod_001",
  "authorizationToken": "pi_3abcdef123456",
  "amount": 100.00,
  "currency": "USD",
  "metadata": {}
}

// Response  
{
  "paymentId": "pay_abc123",
  "providerId": "stripe_prod_001",
  "providerTransactionId": "pi_3voided_xyz789",
  "success": true,
  "metadata": {
    "status": "voided"
  }
}
```

#### **GET /api/bank/providers/{id}/health** - Provider Health Check
```json
// Response
{
  "providerId": "stripe_prod_001",
  "isHealthy": true,
  "message": "Provider is operational",
  "details": {
    "latencyMs": 145,
    "successRate": 99.2,
    "lastCheckAt": "2024-01-15T10:30:00Z"
  }
}
```

### **Interactions with Other Services**

**As Event Consumer:**
- Subscribes to `PaymentRouteSelectedEvent` → Uses selected provider for authorization
- May subscribe to `PaymentFailedEvent` → Updates provider failure metrics

**As Event Publisher:**
- Publishes `PaymentAuthorizationSucceededEvent` → PaymentProcessing completes payment
- Publishes `PaymentAuthorizationFailedEvent` → PaymentProcessing retries or fails
- Publishes `ProviderRateLimitExceededEvent` → PaymentRouter opens circuit breaker
- Publishes `ProviderHealthChangedEvent` → PaymentRouter updates routing decisions

**External Provider Interactions:**
- **Stripe API** → `POST /v1/payment_intents`, `POST /v1/charges/{id}/capture`
- **Adyen API** → `POST /pal/servlet/Payment/authorise`, `POST /pal/servlet/Payment/capture`
- **Bank APIs** → Various bank-specific protocols (SOAP, REST, ISO 8583)
- **Health Checks** → Regular pings to provider endpoints

### **Business Rules & Decision-Making**

#### **Retry Logic**
```
Retry Configuration:
- MaxRetries: 3
- BackoffMs: 1000ms (exponential: 1000ms, 2000ms, 4000ms)
- RetryableErrors: ["timeout", "rate_limit_exceeded", "transient_error"]

Retry Decision:
if (error in RetryableErrors && retry_count < MaxRetries) {
    wait(exponential_backoff_with_jitter());
    retry_request();
} else {
    mark_as_fatal_error();
}
```

#### **Rate Limiting**
```
Per-Provider Rate Limits:
- MaxRequestsPerSecond: 100 (provider-specific)
- MaxConcurrentRequests: 10 (provider-specific)

Rate Limit Enforcement:
if (current_requests >= MaxConcurrentRequests) {
    queue_request();  // Wait for slot
}
if (requests_per_second >= MaxRequestsPerSecond) {
    throttle_request(); // Add delay between requests
}
```

#### **Error Normalization**
```
Provider-Specific Errors → Standard Internal Errors:

Stripe Errors:
  "card_declined" → ProviderError.CardDeclined
  "insufficient_funds" → ProviderError.InsufficientFunds  
  "processing_error" → ProviderError.ProcessingError

Adyen Errors:
  "DECLINED" → ProviderError.CardDeclined
  "REFUSED" → ProviderError.GenericDecline
  "AUTHORISATION_REFUSED" → ProviderError.AuthorizationFailed
```

#### **PCI-DSS Compliance (Sanitization)**
```
Sensitive Data Removal from Logs:
- Credit card numbers: "4242************4242"  
- CVV/CVC: "***"
- PINs: "****"
- API keys: "sk_********************"

Request/Response Logging:
BEFORE logging → Remove all sensitive fields
AFTER sanitization → Store in ProviderRequestLog
```

### **Position in Payment Lifecycle**

```
BankAdapter is the FINAL STEP in payment processing:

1. Provider Selected → PaymentRouter publishes event
2. BankAdapter Receives → Gets provider and configuration
3. Rate Limit Check → Ensure within provider limits
4. Request Logging → Create sanitized log entry
5. Provider Call → Execute with retry logic
6. Response Processing → Normalize errors, update log
7. Event Publishing → Notify payment processing result
8. Return Response → Success/failure to calling service
```

---

## Complete Payment Ecosystem Architecture

### **Service Communication Diagram**

```
┌─────────────────────────────────────────────────────────────────┐
│                     EXTERNAL WORLD                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │   Customer   │  │   Merchant   │  │Payment Providers│       │
│  │              │  │              │  │ (Stripe,Adyen,etc)│       │
│  └──────────────┘  └──────────────┘  └──────────────┘         │
└─────────────────────────────────────────────────────────────────┘
          │                    │                    │
          │ API Requests       │                    │
          ▼                    ▼                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                 PAYMENT PROCESSING SERVICE                        │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              Payment Lifecycle Manager                    │  │
│  │  • Payment creation & state machine                       │  │
│  │  • Idempotency management                                 │  │
│  │  • Retry coordination                                     │  │
│  │  • Domain event publishing                                │  │
│  └───────────────────────────────────────────────────────────┘  │
│                          │                                       │
│                          │ Domain Events                          │
│                          ▼                                       │
└─────────────────────────────────────────────────────────────────┘
                           │
                           │ PaymentCreatedEvent
                           │ PaymentStatusChangedEvent
                           │ PaymentCompletedEvent
                           │ PaymentFailedEvent
                           │
          ┌────────────────┼────────────────┐
          │                │                │
          ▼                ▼                ▼
┌──────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│   PAYMENT       │ │    RISK         │ │   PAYMENT       │
│   ROUTER        │ │   ASSESSMENT    │ │   ADAPTER       │
└──────────────────┘ └─────────────────┘ └─────────────────┘
     │                     │                     │
     │ RouteSelectedEvent  │ RiskEvaluatedEvent │ AuthorizationEvents
     │                     │                     │
     │                     │                     │
     ▼                     ▼                     ▼
┌──────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ External        │ │ Internal        │ │ External        │
│ Providers       │ │ Decision Making │ │ Providers       │
│ (Health Checks) │ │ (Rules/Lists)   │ │ (API Calls)     │
└──────────────────┘ └─────────────────┘ └─────────────────┘
```

### **Event-Driven Architecture**

**Domain Events (Published by PaymentProcessing):**
```csharp
PaymentCreatedEvent        → Triggers routing & risk evaluation
PaymentStatusChangedEvent  → Notifies state changes
PaymentCompletedEvent      → Triggers settlement & notifications  
PaymentFailedEvent         → Triggers retry logic & failure handling
PaymentCancelledEvent     → Triggers cancellation processing
```

**Routing Events (Published by PaymentRouter):**
```csharp
PaymentRouteSelectedEvent → BankAdapter uses selected provider
ProviderHealthChangedEvent → Updates routing decisions
ProviderCircuitOpenedEvent → Emergency routing changes
```

**Risk Events (Published by RiskAssessment):**
```csharp
RiskEvaluationCompletedEvent → PaymentProcessing continues/rejects
SuspiciousActivityDetectedEvent → Security team alerts
```

**BankAdapter Events:**
```csharp
PaymentAuthorizationSucceededEvent → PaymentProcessing completes
PaymentAuthorizationFailedEvent → PaymentProcessing retries
ProviderRateLimitExceededEvent → PaymentRouter opens circuit breaker
```

---

## End-to-End Payment Flow

### **Scenario: Customer Makes Payment for Order**

**Initial State:** Customer has selected items worth $100 USD and chosen to pay with credit card ending in 4242.

#### **Step 1: Payment Creation**

```http
POST /api/payments HTTP/1.1
Content-Type: application/json

{
  "merchantId": "merchant_001",
  "customerId": "customer_abc123", 
  "amount": 100.00,
  "currency": "USD",
  "idempotencyKey": "order-123-2024-01-15",
  "paymentMethodToken": "pm_visa_4242",
  "metadata": {
    "orderId": "order_123",
    "customerEmail": "john@example.com",
    "customerIp": "192.168.1.100"
  }
}
```

**PaymentProcessing Response:**
```json
{
  "paymentId": "pay_abc123xyz",
  "status": "Created",
  "amount": 100.00,
  "currency": "USD", 
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Internal Actions:**
- PaymentProcessing creates `Payment` entity with status `Created`
- Stores idempotency key to prevent duplicates
- Publishes `PaymentCreatedEvent`

---

#### **Step 2: Concurrent Risk Evaluation & Provider Routing**

*Both RiskAssessment and PaymentRouter receive `PaymentCreatedEvent` simultaneously*

**2A. Risk Assessment:**

```http
POST /api/risk/evaluate HTTP/1.1
Content-Type: application/json

{
  "paymentId": "pay_abc123xyz",
  "merchantId": "merchant_001",
  "customerId": "customer_abc123",
  "amount": 100.00,
  "currency": "USD",
  "countryCode": "US",
  "ipAddress": "192.168.1.100",
  "customerEmail": "john@example.com"
}
```

**RiskAssessment Response (APPROVED):**
```json
{
  "evaluationId": "risk_eval_456",
  "paymentId": "pay_abc123xyz",
  "riskScore": 15,
  "decision": "APPROVE",
  "evaluatedAt": "2024-01-15T10:30:40Z",
  "triggeredRules": [
    {
      "ruleId": "amount_check",
      "ruleName": "Amount Threshold Check", 
      "action": "SCORE_ONLY",
      "scoreImpact": 15,
      "description": "Transaction amount > $50"
    }
  ]
}
```

**Internal Risk Evaluation:**
- Checks whitelist → Customer not found
- Checks blacklist → Customer not found  
- Checks velocity limits → Customer has 2 transactions in last hour (limit: 10) ✅
- Runs rule engine → Only amount rule triggered (+15 points)
- Makes decision → Score 15 < ManualReviewThreshold (50) → **APPROVE**
- Publishes `RiskEvaluationCompletedEvent`

**2B. Provider Routing:**

```http
POST /api/router/route HTTP/1.1
Content-Type: application/json

{
  "paymentId": "pay_abc123xyz",
  "merchantId": "merchant_001",
  "amount": 100.00,
  "currency": "USD", 
  "paymentMethod": "CreditCard",
  "countryCode": "US",
  "strategy": "LowestCost"
}
```

**PaymentRouter Response:**
```json
{
  "decisionId": "route_xyz789",
  "paymentId": "pay_abc123xyz", 
  "selectedProviderId": "stripe_prod_001",
  "providerName": "Stripe Production",
  "alternativeProviderIds": ["adyen_prod_001"],
  "strategy": "LowestCost",
  "decisionReason": "Selected lowest cost provider: $3.20 vs $3.50",
  "costEstimate": {
    "amount": 3.20,
    "currency": "USD"
  }
}
```

**Internal Routing Logic:**
- Gets all enabled providers → Stripe (priority 1), Adyen (priority 2)
- Filters by constraints → Both support USD, CreditCard, $100 amount
- Checks circuit breakers → Both healthy (CLOSED state)
- Calculates costs:
  - Stripe: $0.30 + (100 × 2.9%) = $3.20
  - Adyen: $0.35 + (100 × 3.15%) = $3.50
- Selects Stripe (lowest cost)
- Publishes `PaymentRouteSelectedEvent`

---

#### **Step 3: Payment Authorization**

*PaymentProcessing receives both approval events and proceeds to authorization*

**Payment Processing State Update:**
- PaymentProcessing transitions payment status: `Created` → `Processing` → `RiskEvaluation` → `Routing` → `Authorizing`
- Publishes `PaymentStatusChangedEvent`

**BankAdapter Authorization Call:**

```http
POST /api/bank/authorize HTTP/1.1
Content-Type: application/json

{
  "paymentId": "pay_abc123xyz",
  "providerId": "stripe_prod_001",
  "amount": 100.00,
  "currency": "USD",
  "metadata": {
    "customerId": "customer_abc123",
    "paymentMethodToken": "pm_visa_4242",
    "orderId": "order_123"
  }
}
```

**BankAdapter Internal Processing:**
1. Gets provider configuration for `stripe_prod_001`
2. Checks rate limits → Currently 3/100 requests per second ✅
3. Checks concurrent requests → Currently 2/10 concurrent ✅
4. Creates request log entry (sanitized)
5. Calls Stripe API with retry logic:
   ```
   POST https://api.stripe.com/v1/payment_intents
   {
     "amount": 10000,  // $100.00 in cents
     "currency": "usd",
     "payment_method": "pm_visa_4242",
     "capture_method": "manual"  // Authorization only
   }
   ```
6. Stripe responds: `200 OK` with `pi_3abcdef123456`

**BankAdapter Response (Success):**
```json
{
  "paymentId": "pay_abc123xyz",
  "providerId": "stripe_prod_001",
  "providerTransactionId": "pi_3abcdef123456",
  "success": true,
  "metadata": {
    "status": "requires_capture",
    "clientSecret": "pi_3abcdef123456_secret_xyz789",
    "amount": 10000,
    "currency": "usd"
  }
}
```

**Internal BankAdapter Actions:**
- Updates request log as successful (duration: 245ms, HTTP 200)
- Publishes `PaymentAuthorizationSucceededEvent`

---

#### **Step 4: Payment Completion**

*PaymentProcessing receives success event and completes payment*

**PaymentProcessing Final Actions:**
- Records payment attempt: Attempt #1, Provider: stripe, Status: Success
- Transitions payment status: `Authorizing` → `Completed`
- Sets `CompletedAt` timestamp
- Publishes `PaymentCompletedEvent`

**Final Payment Response:**
```json
{
  "paymentId": "pay_abc123xyz",
  "status": "Completed",
  "amount": 100.00,
  "currency": "USD",
  "customerId": "customer_abc123",
  "merchantId": "merchant_001",
  "createdAt": "2024-01-15T10:30:00Z",
  "completedAt": "2024-01-15T10:31:30Z",
  "attempts": [
    {
      "attemptNumber": 1,
      "provider": "stripe",
      "status": "Success",
      "attemptedAt": "2024-01-15T10:31:05Z"
    }
  ],
  "stateTransitions": [
    {"from": "Created", "to": "Processing", "at": "2024-01-15T10:30:10Z"},
    {"from": "Processing", "to": "RiskEvaluation", "at": "2024-01-15T10:30:15Z"},
    {"from": "RiskEvaluation", "to": "Routing", "at": "2024-01-15T10:30:45Z"},
    {"from": "Routing", "to": "Authorizing", "at": "2024-01-15T10:31:00Z"},
    {"from": "Authorizing", "to": "Completed", "at": "2024-01-15T10:31:30Z"}
  ]
}
```

---

### **Alternative Flows**

#### **Flow A: Payment Declined by Provider**

**Step 3 (Modified): BankAdapter receives card decline from Stripe**

```json
// Stripe API Response
{
  "error": {
    "code": "card_declined",
    "message": "Your card was declined.",
    "type": "card_error"
  }
}
```

**BankAdapter Response (Failure):**
```json
{
  "paymentId": "pay_abc123xyz",
  "providerId": "stripe_prod_001", 
  "success": false,
  "errorCode": "card_declined",
  "errorMessage": "Your card was declined.",
  "metadata": {}
}
```

**PaymentProcessing Retry Logic:**
- Records failed attempt: Attempt #1, Provider: stripe, Status: Failed
- Increments `RetryCount` to 1
- Checks `RetryCount < 3` → Proceeds with retry
- Transitions status: `Authorizing` → `Failed` (temporary)
- Requests new routing decision for alternative provider
- Routes to Adyen (alternative provider)
- Attempts authorization with Adyen
- If Adyen also declines → `RetryCount` becomes 2
- After 3rd failure → Marks payment as permanently `Failed`
- Publishes `PaymentFailedEvent`

#### **Flow B: Risk Evaluation Rejection**

**Step 2A (Modified): RiskAssessment detects fraud**

```json
// RiskAssessment Response
{
  "evaluationId": "risk_eval_789",
  "paymentId": "pay_abc123xyz",
  "riskScore": 85,
  "decision": "REJECT",
  "evaluatedAt": "2024-01-15T10:30:40Z",
  "triggeredRules": [
    {
      "ruleId": "velocity_exceeded",
      "ruleName": "Velocity Limit Check",
      "action": "BLOCK", 
      "scoreImpact": 50,
      "description": "Customer exceeded 5 transactions per hour limit"
    },
    {
      "ruleId": "suspicious_ip",
      "ruleName": "Suspicious IP Address",
      "action": "BLOCK",
      "scoreImpact": 35,
      "description": "IP address matches known botnet"
    }
  ]
}
```

**PaymentProcessing Actions:**
- Transitions status: `Processing` → `RiskEvaluation` → `Failed`
- Sets `FailureReason: "Risk score 85 exceeded auto-reject threshold of 80"`
- Does NOT proceed to routing or authorization
- Publishes `PaymentFailedEvent`
- Sends rejection notification to customer

#### **Flow C: Provider Circuit Breaker**

**Step 2B (Modified): PaymentRouter detects Stripe is down**

**Internal State:** Stripe circuit breaker is OPEN due to 5 consecutive failures

**PaymentRouter Response:**
```json
{
  "decisionId": "route_xyz789",
  "paymentId": "pay_abc123xyz",
  "selectedProviderId": "adyen_prod_001",
  "providerName": "Adyen Production",
  "alternativeProviderIds": ["bank_001"],
  "strategy": "PriorityBased",
  "decisionReason": "Primary provider Stripe unavailable (circuit breaker open), using backup provider",
  "costEstimate": {
    "amount": 3.50,
    "currency": "USD"
  }
}
```

**Flow continues with Adyen instead of Stripe.**

#### **Flow D: Customer Cancellation**

**Cancellation Request:**
```http
DELETE /api/payments/pay_abc123xyz HTTP/1.1
```

**PaymentProcessing Actions:**
- Checks current status: `Processing` (can cancel) ✅
- Transitions status: `Processing` → `Cancelled`
- Publishes `PaymentCancelledEvent`
- RiskAssessment and PaymentRouter cancel their evaluations
- If authorization already started, BankAdapter attempts void

---

## Service Interaction Summary

### **Critical Dependencies**

```
PaymentProcessing (Orchestrator)
    ↓ (Publishes events)
    ├─→ PaymentRouter (Provider Selection)
    ├─→ RiskAssessment (Fraud Detection) 
    └─→ BankAdapter (Provider Integration)
         ↓ (Returns results)
    PaymentProcessing (Completes workflow)
```

### **Timeline of Typical Payment**

```
T+0s    Payment Created → PaymentProcessing
T+0.5s  Risk Evaluation Started → RiskAssessment
T+0.5s  Provider Routing Started → PaymentRouter
T+1.5s  Risk Approved → PaymentProcessing
T+2.0s  Provider Selected → PaymentProcessing
T+2.5s  Authorization Started → BankAdapter
T+3.0s  Authorization Complete → PaymentProcessing
T+3.5s  Payment Completed → Customer notified

Total: ~3.5 seconds for successful payment
```

### **What's Missing for Production-Grade System**

**Current System (Educational):**
- ✅ Core payment processing logic
- ✅ Service communication patterns
- ✅ Event-driven architecture
- ✅ State management and retries
- ✅ Basic fraud detection
- ✅ Provider abstraction

**Missing for Production:**
- ❌ Actual message broker (RabbitMQ/Kafka) - currently in-memory
- ❌ Distributed transactions and sagas
- ❌ Customer and payment method management
- ❌ Settlement and bank reconciliation
- ❌ Comprehensive compliance and regulatory features
- ❌ Advanced fraud detection with ML
- ❌ Webhook management for async notifications
- ❌ Complex fee structures and merchant accounts
- ❌ Chargeback and dispute management
- ❌ PCI-DSS Level 1 certification requirements
- ❌ Multi-currency and cross-border payments
- ❌ Recurring payment and subscription support

**Educational Value:**
This system provides excellent foundation for understanding payment processing concepts, service interactions, and domain modeling. The patterns and architecture are sound for learning purposes, but would require significant enhancement for production use.

---

**Next Steps:** Based on this analysis, we can now design improvements and enhancements to make this system more comprehensive while maintaining educational clarity.

**Customer**
```
// POST /api/customers - Create new customer
{
  "merchantId": "merchant_001",
  "email": "john@example.com",
  "name": "John Smith",
  "phone": "+1-555-0123"
}

// POST /api/customers/{customerId}/payment-methods - Add payment method
{
  "paymentMethodToken": "pm_visa_4242",  // From Stripe SDK
  "setDefault": true
}

// GET /api/customers/{customerId}/payment-methods - List saved methods
[
  {
    "paymentMethodId": "pm_visa_4242",
    "type": "CreditCard",
    "last4": "4242",
    "brand": "Visa",
    "isDefault": true
  },
  {
    "paymentMethodId": "pm_mastercard_5678",
    "type": "CreditCard", 
    "last4": "5678",
    "brand": "MasterCard",
    "isDefault": false
  }
]

// DELETE /api/customers/{customerId}/payment-methods/{methodId} - Remove method
```