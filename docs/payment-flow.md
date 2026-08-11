# End-to-End Payment Flow & Reconciliation Process

## Table of Contents
1. [Payment Flow Overview](#payment-flow-overview)
2. [User Initiated Transfer Flow](#user-initiated-transfer-flow)
3. [Backend Service Orchestration](#backend-service-orchestration)
4. [Validation & Security Checks](#validation--security-checks)
5. [Transaction Creation & State Transitions](#transaction-creation--state-transitions)
6. [Bank Integration & Gateway Processing](#bank-integration--gateway-processing)
7. [Scenario Handling](#scenario-handling)
8. [Notification System](#notification-system)
9. [Reconciliation Process](#reconciliation-process)
10. [Domain Entities](#domain-entities)
11. [Service Responsibilities](#service-responsibilities)
12. [Business Rules](#business-rules)

---

## Payment Flow Overview

The payment system follows a **distributed microservices architecture** with Domain-Driven Design (DDD) principles. Each service owns its bounded context and communicates via synchronous HTTP APIs and asynchronous events.

### System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              API Gateway                                      │
│                    (Auth, Rate Limit, Routing, Validation)                   │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │
                                ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Payment Processing Service                            │
│                         (Orchestration, State Machine)                        │
└───┬───────────────┬───────────────┬───────────────┬─────────────────────────┘
    │               │               │               │
    ↓               ↓               ↓               ↓
┌────────┐    ┌────────┐    ┌────────┐    ┌──────────────────┐
│  Risk  │    │ Router │    │  Bank  │    │      Ledger      │
│Service │    │Service │    │Adapter │    │     Service      │
└────────┘    └────────┘    └────────┘    │  (Double-Entry)  │
                                   ┌──────┴──────────────────┘
                                   │
                                   ↓
                            ┌──────────────┐
                            │Notification  │
                            │   Service   │
                            └──────────────┘
```

### Service Communication Patterns

| Pattern | Use Case | Services |
|---------|----------|----------|
| **Synchronous HTTP** | Request/Response operations | Gateway ↔ Payment, Payment ↔ Risk |
| **Asynchronous Events** | Loose coupling, eventual consistency | All services via message broker |
| **Saga Pattern** | Distributed transaction coordination | Payment orchestrates multi-step flow |
| **Circuit Breaker** | Fault tolerance, resilience | Router → Bank Adapter |

---

## User Initiated Transfer Flow

### Sequence Diagram: User Initiates Money Transfer

```
User          Web/Mobile      API         Payment       Risk       Router      Bank     Ledger    Notification
App           App            Gateway     Processing    Service    Service    Adapter   Service     Service
│              │              │            │            │          │          │         │          │
│─Transfer────>│              │            │            │          │          │         │          │
│              │─POST /api/v1/payments──>│            │          │          │         │          │
│              │              │─Auth+Validate──>│          │          │         │          │
│              │              │<─Token OK────│            │          │          │         │          │
│              │              │─Route to Payment Service──────>│          │         │          │
│              │              │            │          │Create Payment Record        │         │          │
│              │              │            │─PaymentCreated Event───────────────────────────────────────>│
│              │              │            │          │          │         │Create Pending JE        │
│              │              │            │          │          │         │         │          │
│              │              │            │─Evaluate Risk──────────────>│          │         │          │
│              │              │            │          │─Velocity Check          │         │          │
│              │              │            │          │─Blacklist Check         │         │          │
│              │              │            │          │─GeoLocation Check       │         │          │
│              │              │            │          │─Calculate Risk Score    │         │          │
│              │              │            │<─────RiskEvaluationCompleted──│          │         │          │
│              │              │            │          │          │         │         │          │
│              │              │            │─Route Payment─────────────────────────>│         │          │
│              │              │            │          │          │─Select Provider        │          │
│              │              │            │          │          │─Check Health            │          │
│              │              │            │          │<─────────Route Selected───│         │          │
│              │              │            │          │          │         │         │          │
│              │              │            │─Authorize────────────────────────────────────────>│
│              │              │            │          │          │         │─Bank API Call──>│
│              │              │            │          │          │         │<─Auth Response──│
│              │              │            │          │          │<─────────AuthSucceeded──│         │
│              │              │            │─PaymentCompleted Event────────────────────────────────────>│
│              │              │            │          │          │         │─Post Journal Entry          │
│              │              │            │          │          │         │<──────PostedJE──────────────│
│              │              │            │          │          │         │         │          │
│              │              │            │          │          │         │         │─Send Success Notification
│              │              │            │          │          │         │         │          │
│              │<─202 OK─────│              │            │          │          │         │         │─Email/SMS──>│
│              │─Display success           │            │          │          │         │         │          │
│<─Success UI─│              │            │          │          │         │         │          │
```

### Step-by-Step User Flow

#### 1. User Interface Interactions

**Web/Mobile App Actions:**

| Action | UI Component | User Input | Validation |
|--------|---------------|------------|------------|
| **Enter Amount** | Amount field | Decimal value | Must be > 0, max 10M |
| **Select Bank** | Bank dropdown | Bank ID | Must be enabled bank |
| **Add Note** (optional) | Note field | Free text | Max 500 chars |
| **Review Transfer** | Summary screen | Confirm details | Read-only confirmation |
| **Authenticate** | Biometric/PIN | Security credential | Device-based auth |
| **Confirm Transfer** | Submit button | Final approval | Triggers payment request |

**Request Payload:**
```json
{
  "amount": {
    "value": "150.00",
    "currency": "USD"
  },
  "destinationBank": "CHASE",
  "accountNumber": "****1234",
  "accountType": "CHECKING",
  "note": "Rent payment for March 2026",
  "idempotencyKey": "client-gen-uuid-v4",
  "metadata": {
    "ipAddress": "192.168.1.100",
    "userAgent": "Mozilla/5.0...",
    "deviceId": "device-fingerprint-id"
  }
}
```

#### 2. API Gateway Processing

**API Gateway Responsibilities:**

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant AuthProvider
    
    Client->>Gateway: POST /api/v1/payments
    Gateway->>AuthProvider: Validate Bearer Token
    AuthProvider-->>Gateway: Token Valid + Merchant Context
    
    Gateway->>Gateway: Check Rate Limits (per merchant)
    Gateway->>Gateway: Validate Request Schema
    Gateway->>Gateway: Enforce Request Size Limit (<1MB)
    Gateway->>Gateway: Extract Trace Headers
    Gateway->>Payment Processing: Route to Payment Service
```

**Security Checks at Gateway:**

| Check | Mechanism | Threshold | Action on Violation |
|-------|-----------|-----------|---------------------|
| **Authentication** | OAuth2/OIDC Bearer Token | Valid token required | 401 Unauthorized |
| **Rate Limiting** | Token Bucket Algorithm | 1000 req/min per merchant | 429 Too Many Requests |
| **Request Size** | Content-Length Header | Max 1MB | 413 Payload Too Large |
| **Schema Validation** | JSON Schema | Required fields present | 400 Bad Request |
| **TLS Version** | TLS 1.3 | Minimum TLS 1.2 | Connection Rejection |

**Headers Added by Gateway:**
```http
X-Merchant-ID: merchant_abc123
X-User-ID: user_xyz789
X-Trace-ID: trace_uuid_v4
X-Request-ID: request_uuid_v4
```

---

## Backend Service Orchestration

### Payment Processing Service - State Machine

The Payment Processing Service orchestrates the entire payment flow using a **deterministic state machine**:

```
                    ┌──────────────┐
                    │   CREATED    │  ◄── Initial State
                    └──────┬───────┘
                           │
                           ↓
                    ┌──────────────┐
                    │  PROCESSING  │  ── Start Orchestration
                    └──────┬───────┘
                           │
                           ↓
                    ┌──────────────┐
                    │RISK_EVALUATION│  ── Risk Check
                    └──────┬───────┘
                           │
                ┌──────────┴──────────┐
                │                      │
           Approved                 Rejected
                │                      │
                ↓                      ↓
    ┌───────────────┐         ┌──────────────┐
    │    ROUTING    │         │   FAILED     │
    └───────┬───────┘         └──────────────┘
            │
            ↓
    ┌───────────────┐
    │  AUTHORIZING  │  ── Bank Authorization
    └───────┬───────┘
            │
    ┌───────┴────────┐
    │                │
 Success          Failure
    │                │
    ↓                ↓
┌─────────┐    ┌─────────┐
│COMPLETED│    │ FAILED  │  ── Max Retries Exceeded
└─────────┘    └────┬────┘
                   │
              (Retry possible if
               attempts < 3)
```

**State Transitions Table:**

| Current State | Valid Next States | Transition Event | Async? |
|---------------|-------------------|------------------|--------|
| CREATED | PROCESSING | PaymentCreated | Yes |
| PROCESSING | RISK_EVALUATION | OrchestrationStart | No |
| RISK_EVALUATION | ROUTING | RiskApproved | Yes |
| RISK_EVALUATION | FAILED | RiskRejected | Yes |
| ROUTING | AUTHORIZING | RouteSelected | Yes |
| AUTHORIZING | COMPLETED | AuthSucceeded | Yes |
| AUTHORIZING | FAILED | AuthFailed | Yes |
| FAILED | AUTHORIZING | RetryTriggered | No (max 3) |
| Any | CANCELLED | CancelRequested | No (before COMPLETED) |

### Database Operations per Service

#### Payment Processing Service Database

**Payment Table (payments):**
```sql
CREATE TABLE payments (
    payment_id UUID PRIMARY KEY,
    merchant_id VARCHAR(50) NOT NULL,
    customer_id VARCHAR(50) NOT NULL,
    amount DECIMAL(19,4) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    status VARCHAR(20) NOT NULL,
    idempotency_key VARCHAR(100) UNIQUE NOT NULL,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    completed_at TIMESTAMP,
    failure_reason TEXT,
    metadata JSONB,
    CONSTRAINT fk_merchant FOREIGN KEY (merchant_id) REFERENCES merchants(merchant_id)
);

-- Indexes for performance
CREATE INDEX idx_payments_merchant ON payments(merchant_id);
CREATE INDEX idx_payments_status ON payments(status);
CREATE INDEX idx_payments_created ON payments(created_at DESC);
CREATE INDEX idx_payments_idempotency ON payments(idempotency_key);
```

**Payment Attempts Table (payment_attempts):**
```sql
CREATE TABLE payment_attempts (
    attempt_id UUID PRIMARY KEY,
    payment_id UUID NOT NULL,
    attempt_number INT NOT NULL,
    started_at TIMESTAMP NOT NULL,
    completed_at TIMESTAMP,
    status VARCHAR(20) NOT NULL,
    failure_reason TEXT,
    provider VARCHAR(50),
    CONSTRAINT fk_payment FOREIGN KEY (payment_id) REFERENCES payments(payment_id)
);
```

**Payment State Transitions Table (payment_state_transitions):**
```sql
CREATE TABLE payment_state_transitions (
    transition_id BIGSERIAL PRIMARY KEY,
    payment_id UUID NOT NULL,
    from_state VARCHAR(20),
    to_state VARCHAR(20) NOT NULL,
    transitioned_at TIMESTAMP NOT NULL,
    transition_reason TEXT,
    actor VARCHAR(100),
    CONSTRAINT fk_payment_trans FOREIGN KEY (payment_id) REFERENCES payments(payment_id)
);
```

#### Ledger Service Database

**Accounts Table:**
```sql
CREATE TABLE accounts (
    account_id UUID PRIMARY KEY,
    account_number VARCHAR(50) UNIQUE NOT NULL,
    account_type VARCHAR(20) NOT NULL, -- ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE
    account_subtype VARCHAR(50),
    owner_id VARCHAR(50) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    current_balance DECIMAL(19,4) NOT NULL DEFAULT 0,
    available_balance DECIMAL(19,4) NOT NULL DEFAULT 0,
    is_overdraft_allowed BOOLEAN DEFAULT FALSE,
    overdraft_limit DECIMAL(19,4),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);
```

**Journal Entries Table:**
```sql
CREATE TABLE journal_entries (
    entry_id UUID PRIMARY KEY,
    entry_date TIMESTAMP NOT NULL,
    entry_number VARCHAR(50) UNIQUE NOT NULL,
    reference_type VARCHAR(20) NOT NULL, -- PAYMENT, REFUND, CHARGEBACK, ADJUSTMENT
    reference_id UUID NOT NULL,
    description TEXT,
    status VARCHAR(20) NOT NULL, -- PENDING, POSTED, REVERSED
    total_debit DECIMAL(19,4) NOT NULL,
    total_credit DECIMAL(19,4) NOT NULL,
    posted_at TIMESTAMP,
    created_by VARCHAR(100) NOT NULL,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    CONSTRAINT chk_balanced CHECK (total_debit = total_credit)
);
```

**Journal Entry Lines Table:**
```sql
CREATE TABLE journal_entry_lines (
    line_id UUID PRIMARY KEY,
    entry_id UUID NOT NULL,
    account_id UUID NOT NULL,
    debit_amount DECIMAL(19,4) DEFAULT 0,
    credit_amount DECIMAL(19,4) DEFAULT 0,
    description TEXT,
    line_sequence INT NOT NULL,
    CONSTRAINT fk_entry FOREIGN KEY (entry_id) REFERENCES journal_entries(entry_id),
    CONSTRAINT fk_account FOREIGN KEY (account_id) REFERENCES accounts(account_id),
    CONSTRAINT chk_one_amount CHECK (
        (debit_amount > 0 AND credit_amount = 0) OR
        (credit_amount > 0 AND debit_amount = 0)
    )
);
```

---

## Validation & Security Checks

### Multi-Layer Validation Framework

#### Layer 1: API Gateway Validation

```typescript
interface GatewayValidation {
  // Request-level checks
  validateRequestSize(request: Request): ValidationResult;
  validateContentType(request: Request): ValidationResult;
  validateRequestSchema(payload: unknown): ValidationResult;
  
  // Security checks
  authenticate(request: Request): AuthenticationContext;
  authorize(context: AuthenticationContext): AuthorizationResult;
  checkRateLimit(context: AuthenticationContext): RateLimitResult;
  
  // Routing preparation
  extractTraceContext(request: Request): TraceContext;
  enrichHeaders(context: AuthenticationContext): HeaderMap;
}
```

**Request Validation Rules:**
| Field | Type | Required | Validation Rule |
|-------|------|----------|-----------------|
| amount.value | Decimal | Yes | > 0, < 10,000,000 |
| amount.currency | String | Yes | ISO 4217 code |
| destinationBank | String | Yes | Must be enabled bank |
| accountNumber | String | Yes | Last 4 digits masked |
| accountType | Enum | Yes | CHECKING/SAVINGS |
| note | String | No | Max 500 chars |
| idempotencyKey | UUID | Yes | Unique per merchant |
| metadata.ipAddress | IP | Yes | Valid IPv4/IPv6 |

#### Layer 2: Payment Processing Validation

```typescript
interface PaymentProcessingValidation {
  // Business rule validation
  validatePaymentAmount(amount: Money): ValidationResult;
  validateCurrency(currency: string): ValidationResult;
  validateIdempotency(key: string, merchantId: string): Promise<ValidationResult>;
  tokenizePaymentMethod(methodDetails: PaymentMethod): Promise<TokenizedMethod>;
  
  // State transition validation
  validateStateTransition(current: PaymentStatus, next: PaymentStatus): ValidationResult;
  checkCancellationEligibility(payment: Payment): ValidationResult;
}
```

**Business Rules:**
```typescript
const PAYMENT_VALIDATION_RULES = {
  // Amount validation
  MIN_AMOUNT: 0.01,
  MAX_AMOUNT: 10000000,
  AMOUNT_PRECISION: 2,
  
  // Currency validation
  SUPPORTED_CURRENCIES: ['USD', 'EUR', 'GBP', 'CAD', 'AUD'],
  
  // Idempotency
  IDEMPOTENCY_KEY_TTL: 86400, // 24 hours in seconds
  IDEMPOTENCY_KEY_PREFIX: 'payment_',
  
  // State machine rules
  STATE_TRANSITIONS: {
    CREATED: ['PROCESSING', 'CANCELLED'],
    PROCESSING: ['RISK_EVALUATION', 'FAILED', 'CANCELLED'],
    RISK_EVALUATION: ['ROUTING', 'FAILED'],
    ROUTING: ['AUTHORIZING', 'FAILED'],
    AUTHORIZING: ['COMPLETED', 'FAILED'],
    COMPLETED: [], // Immutable
    FAILED: ['AUTHORIZING'], // Can retry
    CANCELLED: [] // Immutable
  }
};
```

#### Layer 3: Risk Assessment Validation

```typescript
interface RiskAssessmentValidation {
  // Velocity checks
  checkTransactionVelocity(customerId: string): Promise<VelocityCheckResult>;
  
  // Blacklist/whitelist
  checkBlacklist(customerId: string): Promise<boolean>;
  checkWhitelist(customerId: string): Promise<boolean>;
  
  // Geographic validation
  validateCustomerLocation(ipAddress: string, customerId: string): Promise<GeoCheckResult>;
  
  // Amount validation
  validateTransactionAmount(amount: Money, merchantId: string): Promise<AmountCheckResult>;
  
  // Rule engine execution
  evaluateRiskRules(payment: Payment, profile: MerchantRiskProfile): Promise<RiskEvaluation>;
}
```

**Velocity Check Windows:**
```typescript
const VELOCITY_WINDOWS = {
  '1_MINUTE': { max: 3, window: 60 },      // Max 3 tx in 1 minute
  '5_MINUTE': { max: 10, window: 300 },    // Max 10 tx in 5 minutes
  '1_HOUR': { max: 30, window: 3600 },     // Max 30 tx in 1 hour
  '1_DAY': { max: 100, window: 86400 }     // Max 100 tx in 1 day
};

interface VelocityCheckResult {
  window: string;
  count: number;
  limit: number;
  exceeded: boolean;
  blocked: boolean;
  resetAt: DateTime;
}
```

**Risk Score Calculation:**
```typescript
const RISK_SCORE_CALCULATION = {
  BASE_SCORE: 0,
  
  // Rule-based scoring
  VELOCITY_EXCEEDED: +30,
  BLACKLISTED: +100,  // Auto-reject
  HIGH_RISK_COUNTRY: +20,
  AMOUNT_ABOVE_THRESHOLD: +15,
  NEW_CUSTOMER: +10,
  SUSPICIOUS_PATTERN: +40,
  
  // ML model score (if available)
  ML_MODEL_WEIGHT: 0.5,
  
  // Decision thresholds
  thresholds: {
    AUTO_REJECT: 80,
    MANUAL_REVIEW: 50,
    AUTO_APPROVE: 50
  }
};
```

#### Layer 4: Router Service Validation

```typescript
interface RouterValidation {
  // Provider validation
  validateProviderHealth(providerId: string): boolean;
  validateProviderCapability(provider: PaymentProvider, payment: Payment): boolean;
  
  // Circuit breaker check
  checkCircuitBreaker(providerId: string): CircuitBreakerState;
  
  // Cost calculation
  calculateCost(provider: PaymentProvider, amount: Money): CostEstimate;
  
  // Routing decision
  selectProvider(payment: Payment, rules: RoutingRule[]): RoutingDecision;
}
```

**Provider Capability Validation:**
```typescript
interface ProviderCapability {
  supportedCurrencies: string[];
  supportedMethods: PaymentMethod[];
  minAmount: Money;
  maxAmount: Money;
  processingTime: ProcessingTime;
  successRate: number;
}

function validateCapability(
  provider: PaymentProvider, 
  payment: Payment
): ValidationResult {
  const errors: string[] = [];
  
  if (!provider.supportedCurrencies.includes(payment.currency)) {
    errors.push(`Currency ${payment.currency} not supported`);
  }
  
  if (!provider.supportedMethods.includes(payment.method)) {
    errors.push(`Payment method ${payment.method} not supported`);
  }
  
  if (payment.amount.value < provider.minAmount.value) {
    errors.push(`Amount below minimum ${provider.minAmount}`);
  }
  
  if (payment.amount.value > provider.maxAmount.value) {
    errors.push(`Amount above maximum ${provider.maxAmount}`);
  }
  
  return {
    valid: errors.length === 0,
    errors
  };
}
```

#### Layer 5: Bank Adapter Security

```typescript
interface BankAdapterSecurity {
  // Credential management
  getCredentials(providerId: string): Promise<EncryptedCredentials>;
  decryptCredentials(encrypted: EncryptedCredentials): ProviderCredentials;
  
  // Request sanitization
  sanitizeRequest(request: ProviderRequest): SanitizedRequest;
  sanitizeResponse(response: ProviderResponse): SanitizedResponse;
  
  // PCI-DSS compliance
  maskCardholderData(data: string): string;
  validatePciDssCompliance(logEntry: string): boolean;
}
```

**PCI-DSS Compliance Rules:**
```typescript
const PCI_DSS_MASKING_RULES = {
  // Never log
  NEVER_LOG: [
    'cardNumber',
    'cvv',
    'pin',
    'password',
    'ssn'
  ],
  
  // Mask with asterisks
  MASK_PARTIAL: {
    'accountNumber': (value: string) => {
      if (value.length >= 4) {
        return '*'.repeat(value.length - 4) + value.slice(-4);
      }
      return '****';
    },
    'routingNumber': (value: string) => '***' + value.slice(-3),
    'email': (value: string) => {
      const [name, domain] = value.split('@');
      return name[0] + '***@' + domain;
    }
  },
  
  // Allowed to log
  ALLOWED: [
    'transactionId',
    'amount',
    'currency',
    'status',
    'timestamp',
    'responseCode'
  ]
};
```

---

## Transaction Creation & State Transitions

### Payment Creation Flow

#### Step 1: Payment Record Creation

**API Call:**
```http
POST /api/v1/payments HTTP/1.1
Host: api.payment-system.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Merchant-ID: merchant_abc123
X-Idempotency-Key: payment_550e8400-e29b-41d4-a716-446655440000
Content-Type: application/json

{
  "amount": {
    "value": "150.00",
    "currency": "USD"
  },
  "destinationBank": "CHASE",
  "accountNumber": "****1234",
  "accountType": "CHECKING",
  "note": "Rent payment for March 2026"
}
```

**Response:**
```http
HTTP/1.1 202 Accepted
X-Request-ID: req-123e4567-e89b-12d3-a456-426614174000
X-Trace-ID: trace-7b8f9a0b-1c2d-3e4f-5a6b-7c8d9e0f1a2b
Content-Type: application/json

{
  "paymentId": "pay_550e8400-e29b-41d4-a716-446655440000",
  "status": "CREATED",
  "amount": {
    "value": "150.00",
    "currency": "USD"
  },
  "createdAt": "2026-08-06T10:15:30Z",
  "estimatedCompletion": "2026-08-06T10:17:30Z",
  "links": {
    "self": "/api/v1/payments/pay_550e8400-e29b-41d4-a716-446655440000",
    "status": "/api/v1/payments/pay_550e8400-e29b-41d4-a716-446655440000/status"
  }
}
```

#### Step 2: Idempotency Check

```typescript
async function createPaymentWithIdempotency(
  request: CreatePaymentRequest,
  merchantId: string
): Promise<CreatePaymentResponse> {
  
  const idempotencyKey = request.idempotencyKey;
  
  // Check for existing payment with same idempotency key
  const existingPayment = await paymentRepository.findByIdempotencyKey(
    merchantId, 
    idempotencyKey
  );
  
  if (existingPayment) {
    // Return existing payment without creating duplicate
    return {
      paymentId: existingPayment.paymentId,
      status: existingPayment.status,
      amount: existingPayment.amount,
      createdAt: existingPayment.createdAt,
      isIdempotentRetry: true
    };
  }
  
  // Create new payment
  const payment = await paymentRepository.create({
    paymentId: generateUuid(),
    merchantId,
    customerId: request.customerId,
    amount: request.amount,
    currency: request.currency,
    status: PaymentStatus.CREATED,
    idempotencyKey,
    createdAt: new Date(),
    updatedAt: new Date()
  });
  
  // Publish PaymentCreated event
  await eventBus.publish('PaymentCreated', {
    paymentId: payment.paymentId,
    merchantId: payment.merchantId,
    amount: payment.amount,
    currency: payment.currency,
    status: payment.status
  });
  
  return {
    paymentId: payment.paymentId,
    status: payment.status,
    amount: payment.amount,
    createdAt: payment.createdAt
  };
}
```

#### Step 3: State Transition Logging

```typescript
async function transitionPaymentState(
  paymentId: string,
  newState: PaymentStatus,
  reason: string,
  actor: string
): Promise<void> {
  
  const payment = await paymentRepository.findById(paymentId);
  
  // Validate state transition
  const validTransitions = STATE_TRANSITIONS[payment.status] || [];
  if (!validTransitions.includes(newState)) {
    throw new InvalidStateTransitionError(
      `Cannot transition from ${payment.status} to ${newState}`
    );
  }
  
  // Record state transition in audit log
  await stateTransitionRepository.create({
    transitionId: generateId(),
    paymentId,
    fromState: payment.status,
    toState: newState,
    transitionedAt: new Date(),
    transitionReason: reason,
    actor
  });
  
  // Update payment status
  await paymentRepository.update(paymentId, {
    status: newState,
    updatedAt: new Date()
  });
  
  // Publish state change event
  await eventBus.publish(`PaymentState${newState}`, {
    paymentId,
    previousState: payment.status,
    newState,
    reason
  });
}
```

### Event Flow Architecture

```mermaid
graph TB
    A[PaymentCreated] --> B[RiskEvaluationRequested]
    B --> C{Risk Decision}
    C -->|Approved| D[PaymentRiskApproved]
    C -->|Rejected| E[PaymentRiskRejected]
    D --> F[RoutePaymentRequested]
    F --> G[PaymentRouteSelected]
    G --> H[AuthorizationRequested]
    H --> I{Authorization Result}
    I -->|Success| J[PaymentCompleted]
    I -->|Failure| K[PaymentAuthorizationFailed]
    K -->|Retries < 3| L[RetryPayment]
    L --> H
    K -->|Retries = 3| M[PaymentFailed]
    J --> N[LedgerEntryPosted]
    J --> O[NotificationDelivered]
    M --> O
    E --> O
```

### Event Definitions

```typescript
// Payment Events
interface PaymentCreated {
  eventType: 'PaymentCreated';
  paymentId: string;
  merchantId: string;
  customerId: string;
  amount: Money;
  currency: string;
  idempotencyKey: string;
  createdAt: DateTime;
}

interface PaymentRiskApproved {
  eventType: 'PaymentRiskApproved';
  paymentId: string;
  riskScore: number;
  evaluationId: string;
  triggeredRules: string[];
}

interface PaymentRiskRejected {
  eventType: 'PaymentRiskRejected';
  paymentId: string;
  riskScore: number;
  rejectionReason: string;
  evaluationId: string;
}

interface PaymentRouteSelected {
  eventType: 'PaymentRouteSelected';
  paymentId: string;
  selectedProviderId: string;
  alternativeProviders: string[];
  routingStrategy: string;
  costEstimate: Money;
}

interface PaymentCompleted {
  eventType: 'PaymentCompleted';
  paymentId: string;
  amount: Money;
  currency: string;
  providerTransactionId: string;
  completedAt: DateTime;
}

interface PaymentFailed {
  eventType: 'PaymentFailed';
  paymentId: string;
  failureReason: string;
  attemptNumber: number;
  maxRetriesReached: boolean;
}

// Ledger Events
interface JournalEntryPosted {
  eventType: 'JournalEntryPosted';
  entryId: string;
  entryNumber: string;
  referenceType: 'PAYMENT' | 'REFUND' | 'CHARGEBACK';
  referenceId: string;
  totalDebit: Money;
  totalCredit: Money;
  postedAt: DateTime;
}
```

---

## Bank Integration & Gateway Processing

### Bank Adapter Service Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Bank Adapter Service                          │
│                                                                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │   Stripe    │  │    Adyen     │  │  Bank API    │           │
│  │  Adapter    │  │   Adapter    │  │   Adapter    │           │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘           │
│         │                 │                 │                    │
│         └─────────────────┴─────────────────┘                    │
│                           │                                       │
│                  ┌────────▼────────┐                              │
│                  │  Provider      │                              │
│                  │  Normalizer    │                              │
│                  └────────┬────────┘                              │
│                           │                                       │
│         ┌─────────────────┴──────────────────┐                  │
│         │                                        │                  │
│  ┌──────▼────────┐                     ┌───────▼──────┐           │
│  │ Retry Logic   │                     │ Rate Limiter │           │
│  └──────┬────────┘                     └───────┬──────┘           │
│         │                                      │                  │
│         └──────────────┬───────────────────────┘                  │
│                        │                                          │
│                  ┌─────▼─────┐                                     │
│                  │  Request  │                                     │
│                  │   Logger  │                                     │
│                  └───────────┘                                     │
└─────────────────────────────────────────────────────────────────┘
```

### Authorization Flow

```typescript
async function authorizePayment(
  payment: Payment,
  routingDecision: RoutingDecision
): Promise<AuthorizationResult> {
  
  const provider = await providerRepository.findById(
    routingDecision.selectedProviderId
  );
  
  const requestLog = await createRequestLog({
    paymentId: payment.paymentId,
    providerId: provider.providerId,
    operation: 'AUTHORIZE',
    requestSentAt: new Date()
  });
  
  try {
    // Translate internal request to provider-specific format
    const providerRequest = translateToProviderFormat(payment, provider);
    
    // Apply rate limiting
    await rateLimiter.acquireToken(provider.providerId);
    
    // Execute provider request with retry logic
    const response = await executeWithRetry(
      async () => {
        return await providerHttpClient.post(
          provider.endpoint,
          providerRequest,
          {
            timeout: provider.timeouts.readTimeoutMs,
            auth: await getProviderAuth(provider)
          }
        );
      },
      provider.retryConfig
    );
    
    // Normalize provider response
    const normalizedResponse = normalizeProviderResponse(
      response,
      provider
    );
    
    // Update request log
    await updateRequestLog(requestLog.logId, {
      responseReceivedAt: new Date(),
      duration: calculateDuration(requestLog.requestSentAt),
      responsePayload: sanitizeResponse(response),
      httpStatusCode: response.statusCode,
      result: determineResult(normalizedResponse)
    });
    
    if (normalizedResponse.success) {
      // Publish success event
      await eventBus.publish('PaymentAuthorizationSucceeded', {
        paymentId: payment.paymentId,
        providerTransactionId: normalizedResponse.transactionId,
        authorizedAmount: normalizedResponse.amount,
        authorizedAt: new Date()
      });
      
      return {
        success: true,
        providerTransactionId: normalizedResponse.transactionId,
        authorizedAmount: normalizedResponse.amount,
        responseCode: normalizedResponse.responseCode
      };
    } else {
      // Check if error is retryable
      if (isRetryableError(normalizedResponse.error)) {
        await eventBus.publish('PaymentAuthorizationFailed', {
          paymentId: payment.paymentId,
          failureReason: normalizedResponse.error.message,
          isRetryable: true
        });
        
        return {
          success: false,
          isRetryable: true,
          error: normalizedResponse.error
        };
      } else {
        await eventBus.publish('PaymentAuthorizationFailed', {
          paymentId: payment.paymentId,
          failureReason: normalizedResponse.error.message,
          isRetryable: false
        });
        
        return {
          success: false,
          isRetryable: false,
          error: normalizedResponse.error
        };
      }
    }
    
  } catch (error) {
    // Handle unexpected errors
    await updateRequestLog(requestLog.logId, {
      responseReceivedAt: new Date(),
      error: error.message,
      result: 'FATAL_ERROR'
    });
    
    throw error;
  }
}
```

### Retry Logic with Exponential Backoff

```typescript
interface RetryConfig {
  maxRetries: number;
  initialBackoffMs: number;
  maxBackoffMs: number;
  retryableErrors: string[];
}

async function executeWithRetry<T>(
  operation: () => Promise<T>,
  config: RetryConfig
): Promise<T> {
  
  let lastError: Error;
  let backoffMs = config.initialBackoffMs;
  
  for (let attempt = 0; attempt <= config.maxRetries; attempt++) {
    try {
      return await operation();
      
    } catch (error) {
      lastError = error;
      
      // Check if error is retryable
      if (!isRetryableError(error, config.retryableErrors)) {
        throw error; // Don't retry fatal errors
      }
      
      // Don't sleep after last attempt
      if (attempt < config.maxRetries) {
        // Calculate backoff with jitter
        const jitter = Math.random() * backoffMs * 0.1;
        const sleepTime = Math.min(backoffMs + jitter, config.maxBackoffMs);
        
        logger.info(`Retry attempt ${attempt + 1}/${config.maxRetries}`, {
          error: error.message,
          nextRetryIn: sleepTime
        });
        
        await sleep(sleepTime);
        
        // Exponential backoff
        backoffMs = Math.min(backoffMs * 2, config.maxBackoffMs);
      }
    }
  }
  
  throw lastError;
}

function isRetryableError(error: Error, retryableErrors: string[]): boolean {
  const retryablePatterns = [
    /connection/i,
    /timeout/i,
    /502/i,
    /503/i,
    /504/i,
    /ECONNRESET/i,
    /ETIMEDOUT/i,
    /temporarily unavailable/i
  ];
  
  // Check against whitelist
  if (retryableErrors.some(pattern => error.message.includes(pattern))) {
    return true;
  }
  
  // Check against patterns
  return retryablePatterns.some(pattern => pattern.test(error.message));
}
```

### Circuit Breaker Implementation

```typescript
class CircuitBreaker {
  private state: CircuitState = 'CLOSED';
  private failureCount: number = 0;
  private lastFailureTime?: Date;
  private successCount: number = 0;
  
  constructor(
    private threshold: number = 5,
    private timeoutMs: number = 60000,
    private halfOpenMaxCalls: number = 3
  ) {}
  
  async execute<T>(operation: () => Promise<T>): Promise<T> {
    if (this.state === 'OPEN') {
      if (this.shouldAttemptReset()) {
        this.state = 'HALF_OPEN';
        logger.info('Circuit breaker entering HALF_OPEN state');
      } else {
        throw new CircuitBreakerOpenError('Circuit breaker is OPEN');
      }
    }
    
    try {
      const result = await operation();
      this.onSuccess();
      return result;
      
    } catch (error) {
      this.onFailure();
      throw error;
    }
  }
  
  private onSuccess(): void {
    this.failureCount = 0;
    
    if (this.state === 'HALF_OPEN') {
      this.successCount++;
      
      if (this.successCount >= this.halfOpenMaxCalls) {
        this.state = 'CLOSED';
        this.successCount = 0;
        logger.info('Circuit breaker closed after successful recovery');
      }
    }
  }
  
  private onFailure(): void {
    this.failureCount++;
    this.lastFailureTime = new Date();
    
    if (this.failureCount >= this.threshold) {
      this.state = 'OPEN';
      logger.error('Circuit breaker opened after threshold failures', {
        failures: this.failureCount,
        threshold: this.threshold
      });
      
      // Publish alert event
      eventBus.publish('ProviderCircuitOpened', {
        providerId: this.providerId,
        failureCount: this.failureCount,
        openedAt: this.lastFailureTime
      });
    }
  }
  
  private shouldAttemptReset(): boolean {
    if (!this.lastFailureTime) return false;
    
    const timeSinceLastFailure = Date.now() - this.lastFailureTime.getTime();
    return timeSinceLastFailure >= this.timeoutMs;
  }
}

type CircuitState = 'CLOSED' | 'OPEN' | 'HALF_OPEN';
```

---

## Scenario Handling

### Success Scenario

```
User → Gateway → Payment Service
  │                 │
  │                 ├─ CREATE Payment (status: CREATED)
  │                 │  └─ Publish: PaymentCreated
  │                 │
  │                 ├─ EVALUATE Risk (Risk Service)
  │                 │  └─ Result: APPROVED (score: 15)
  │                 │  └─ Publish: PaymentRiskApproved
  │                 │
  │                 ├─ SELECT Route (Router Service)
  │                 │  └─ Provider: Stripe (cost: $0.29 + 2.9%)
  │                 │  └─ Publish: PaymentRouteSelected
  │                 │
  │                 ├─ AUTHORIZE (Bank Adapter → Stripe)
  │                 │  └─ Request: POST /v1/charges
  │                 │  └─ Response: 200 OK (charge: ch_3Mm...)
  │                 │  └─ Publish: PaymentAuthorizationSucceeded
  │                 │
  │                 ├─ COMPLETE Payment (status: COMPLETED)
  │                 │  └─ Publish: PaymentCompleted
  │                 │
  │                 ├─ POST Journal Entry (Ledger Service)
  │                 │  ├─ Debit: Customer Account $150.00
  │                 │  └─ Credit: Platform Revenue $150.00
  │                 │  └─ Publish: JournalEntryPosted
  │                 │
  │                 └─ SEND Notification (Notification Service)
  │                     └─ Email: Transfer successful
  │                     └─ SMS: Transfer completed
  │
  └─ 202 OK Response
     └─ paymentId, status, amount, completedAt
```

### Failure Scenarios

#### Scenario 1: Risk Rejection

```typescript
// Risk evaluation fails
{
  eventType: 'PaymentRiskRejected',
  paymentId: 'pay_123',
  riskScore: 85,
  rejectionReason: 'High risk score exceeds auto-reject threshold',
  evaluationId: 'eval_456'
}

// Payment state transition
CREATED → PROCESSING → RISK_EVALUATION → FAILED

// Database updates
UPDATE payments 
SET status = 'FAILED', 
    failure_reason = 'Risk evaluation failed: Score 85 exceeds threshold 80',
    updated_at = NOW()
WHERE payment_id = 'pay_123';

// Events published
1. PaymentRiskRejected
2. PaymentFailed
3. NotificationFailed (to customer)

// Notification sent
Subject: "Transfer Failed - Security Review Required"
Body: "Your transfer of $150.00 could not be processed due to security concerns..."
```

#### Scenario 2: Provider Authorization Failure (Retryable)

```typescript
// First attempt fails with timeout
{
  attemptNumber: 1,
  provider: 'stripe',
  error: 'ETIMEDOUT',
  isRetryable: true
}

// Payment state remains AUTHORIZING
status = 'AUTHORIZING'
currentAttempt = 1

// Retry logic triggered
if (isRetryable && currentAttempt < maxRetries) {
  currentAttempt++;
  backoffMs = Math.pow(2, currentAttempt) * 1000; // 2s, 4s, 8s
  
  setTimeout(() => {
    retryAuthorization(payment);
  }, backoffMs);
}

// Second attempt succeeds
{
  attemptNumber: 2,
  provider: 'stripe',
  success: true,
  transactionId: 'ch_3Mm...'
}

// Payment completes successfully
status = 'COMPLETED'
completedAt = NOW()

// Event published
PaymentCompleted {
  paymentId: 'pay_123',
  attempts: 2,
  totalDuration: '3.2s'
}
```

#### Scenario 3: Max Retries Exceeded

```typescript
// All 3 attempts fail
{
  attemptNumber: 3,
  provider: 'stripe',
  error: '500 Internal Server Error',
  isRetryable: true
}

// Check retry limit
if (currentAttempt >= maxRetries) {
  // Mark as permanently failed
  status = 'FAILED';
  failureReason = 'Max retries (3) exceeded. Last error: 500 Internal Server Error';
  
  // Publish events
  publish('PaymentFailed', {
    paymentId: 'pay_123',
    failureReason: 'Max retries exceeded',
    attemptNumber: 3,
    maxRetriesReached: true
  });
  
  // Notify customer
  publish('NotificationFailed', {
    paymentId: 'pay_123',
    reason: 'Provider unavailable after multiple attempts',
    nextAction: 'Please try again later or contact support'
  });
}
```

#### Scenario 4: Insufficient Funds

```typescript
// Provider returns insufficient funds error
{
  error: {
    code: 'balance_insufficient',
    message: 'Insufficient funds in source account',
    type: 'card_error'
  }
}

// This is a fatal error (not retryable)
{
  isRetryable: false,
  failureCategory: 'CUSTOMER_ERROR'
}

// Payment marked as failed
UPDATE payments
SET status = 'FAILED',
    failure_reason = 'Insufficient funds in source account',
    updated_at = NOW()
WHERE payment_id = 'pay_123';

// Notification sent with specific guidance
{
  template: 'payment_failed_insufficient_funds',
  recipient: customer.email,
  data: {
    amount: '$150.00',
    reason: 'insufficient_funds',
    suggestedAction: 'Please ensure sufficient funds are available and retry'
  }
}
```

### Timeout Scenarios

#### Scenario 5: Risk Evaluation Timeout

```typescript
// Risk service doesn't respond within 500ms
{
  timeout: 500,
  actualDuration: 501,
  error: 'Risk evaluation timeout'
}

// Fail-open policy (configurable)
if (riskEvaluation.timeout >= 500) {
  // Business decision: Allow payment with warning
  logger.warn('Risk evaluation timeout - allowing payment', {
    paymentId: 'pay_123',
    timeout: 501
  });
  
  riskDecision = {
    approved: true,
    fallbackReason: 'timeout',
    warning: 'Risk evaluation skipped due to timeout - payment allowed'
  };
  
  // Continue to routing
  proceedToRouting(payment);
}

// Alternative: Fail-closed (more secure)
if (riskEvaluation.timeout >= 500 && config.failClosed) {
  riskDecision = {
    approved: false,
    fallbackReason: 'timeout',
    error: 'Payment rejected due to risk evaluation timeout'
  };
  
  // Reject payment
  markPaymentAsFailed('Risk evaluation timeout - fail-closed policy');
}
```

#### Scenario 6: Provider Request Timeout

```typescript
// Bank adapter doesn't receive response within provider timeout
{
  providerTimeout: 30000, // 30 seconds
  actualDuration: 30001,
  error: 'Provider request timeout'
}

// Provider request timeout is fatal
{
  error: {
    type: 'provider_timeout',
    message: 'Provider did not respond within 30s timeout',
    isRetryable: false // Provider timeout usually indicates downstream issue
  }
}

// Payment fails without retry
UPDATE payments
SET status = 'FAILED',
    failure_reason = 'Provider timeout: No response from Stripe within 30s',
    updated_at = NOW()
WHERE payment_id = 'pay_123';

// Circuit breaker triggered
circuitBreaker.recordFailure('stripe');
if (circuitBreaker.isOpen('stripe')) {
  publish('ProviderCircuitOpened', {
    providerId: 'stripe',
    reason: 'Timeout after 30s',
    openedAt: NOW()
  });
}

// Future payments routed to alternative provider
routingDecision = {
  selectedProvider: 'adyen', // Fallback provider
  reason: 'Primary provider (Stripe) circuit open'
};
```

---

## Notification System

### Notification Flow Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Notification Service                        │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Email     │  │    SMS      │  │    Push      │          │
│  │   Channel   │  │   Channel   │  │   Channel    │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         │                 │                 │                    │
│         └─────────────────┴─────────────────┘                    │
│                           │                                      │
│                  ┌────────▼────────┐                             │
│                  │  Template        │                             │
│                  │  Renderer        │                             │
│                  └────────┬────────┘                             │
│                           │                                      │
│                  ┌────────▼────────┐                             │
│                  │  Preference     │                             │
│                  │  Filter         │                             │
│                  └────────┬────────┘                             │
│                           │                                      │
│                  ┌────────▼────────┐                             │
│                  │  Delivery       │                             │
│                  │  Tracker        │                             │
│                  └────────┬────────┘                             │
│                           │                                      │
│                  ┌────────▼────────┐                             │
│                  │  Retry Manager  │                             │
│                  └─────────────────┘                             │
└─────────────────────────────────────────────────────────────────┘
```

### Notification Types and Templates

#### 1. Payment Success Notification

**Email Template:**
```html
<!-- Template: payment_success.html -->
Subject: Your transfer of {{amount}} was successful

<!DOCTYPE html>
<html>
<body>
  <h1>Transfer Completed ✅</h1>
  
  <p>Hi {{customerName}},</p>
  
  <p>Your transfer has been completed successfully:</p>
  
  <table>
    <tr>
      <td><strong>Amount</strong></td>
      <td>{{amount}} {{currency}}</td>
    </tr>
    <tr>
      <td><strong>To</strong></td>
      <td>{{destinationBank}} account ****{{lastFour}}</td>
    </tr>
    <tr>
      <td><strong>Reference</strong></td>
      <td>{{paymentId}}</td>
    </tr>
    <tr>
      <td><strong>Completed at</strong></td>
      <td>{{completedAt}}</td>
    </tr>
  </table>
  
  {{#if note}}
  <p><strong>Note:</strong> {{note}}</p>
  {{/if}}
  
  <p>Questions? Contact us at support@payment-system.com</p>
</body>
</html>
```

**SMS Template:**
```
Template: payment_success_sms

Your transfer of {{amount}} {{currency}} to {{destinationBank}} (****{{lastFour}}) was successful. Ref: {{paymentId}}. Reply STOP to opt out.
```

#### 2. Payment Failure Notification

**Email Template:**
```html
<!-- Template: payment_failure.html -->
Subject: Action Required: Your transfer could not be completed

<!DOCTYPE html>
<html>
<body>
  <h1>Transfer Failed ❌</h1>
  
  <p>Hi {{customerName}},</p>
  
  <p>We couldn't complete your transfer of {{amount}} {{currency}} to {{destinationBank}}.</p>
  
  <div class="error-box">
    <strong>Reason:</strong> {{failureReason}}
  </div>
  
  <p><strong>What you can do:</strong></p>
  <ul>
    {{#if isRetryable}}
    <li>Retry the transfer in your app</li>
    {{/if}}
    {{#if needsContact}}
    <li>Contact our support team for assistance</li>
    {{/if}}
    {{#if needsFunds}}
    <li>Ensure sufficient funds are available and retry</li>
    {{/if}}
  </ul>
  
  <p>Reference ID: {{paymentId}}</p>
</body>
</html>
```

### Notification Delivery Flow

```typescript
async function sendPaymentNotification(
  eventType: NotificationEventType,
  payment: Payment,
  recipient: Recipient
): Promise<void> {
  
  // Get user preferences
  const preferences = await preferenceRepository.findByUserId(recipient.userId);
  
  // Determine which channels to use
  const channels = determineChannels(eventType, preferences);
  
  // Render template for each channel
  const notifications = await Promise.all(
    channels.map(channel => 
      renderAndCreateNotification(channel, eventType, payment, recipient)
    )
  );
  
  // Send notifications in parallel
  await Promise.all(
    notifications.map(notification => 
      sendNotificationWithRetry(notification)
    )
  );
}

function determineChannels(
  eventType: NotificationEventType,
  preferences: NotificationPreference
): NotificationChannel[] {
  
  const channels: NotificationChannel[] = [];
  
  // Check email preference
  if (preferences.enabledChannels.includes('EMAIL') && 
      shouldSendForEvent(eventType, preferences.emailEnabled)) {
    channels.push('EMAIL');
  }
  
  // Check SMS preference
  if (preferences.enabledChannels.includes('SMS') && 
      shouldSendForEvent(eventType, preferences.smsEnabled)) {
    channels.push('SMS');
  }
  
  // Check push preference
  if (preferences.enabledChannels.includes('PUSH') && 
      shouldSendForEvent(eventType, preferences.pushEnabled)) {
    channels.push('PUSH');
  }
  
  return channels;
}

async function sendNotificationWithRetry(
  notification: Notification
): Promise<void> {
  
  let attempt = 0;
  const maxAttempts = notification.maxAttempts;
  
  while (attempt < maxAttempts) {
    try {
      await sendViaChannel(notification);
      
      // Mark as sent
      await notificationRepository.update(notification.notificationId, {
        status: 'SENT',
        sentAt: new Date(),
        attempts: attempt + 1
      });
      
      // Publish delivery event
      await eventBus.publish('NotificationDelivered', {
        notificationId: notification.notificationId,
        channel: notification.channel,
        deliveredAt: new Date()
      });
      
      return;
      
    } catch (error) {
      attempt++;
      
      if (attempt >= maxAttempts) {
        // Mark as permanently failed
        await notificationRepository.update(notification.notificationId, {
          status: 'FAILED',
          failedAt: new Date(),
          failureReason: error.message,
          attempts: attempt
        });
        
        // Publish failure event
        await eventBus.publish('NotificationFailed', {
          notificationId: notification.notificationId,
          channel: notification.channel,
          failureReason: error.message
        });
        
        // If critical notification, alert operations
        if (notification.priority === 'URGENT') {
          await alertingService.sendAlert({
            type: 'NOTIFICATION_DELIVERY_FAILURE',
            notificationId: notification.notificationId,
            recipient: notification.recipientId,
            attempts: attempt
          });
        }
        
        return;
      }
      
      // Calculate backoff and retry
      const backoffMs = Math.pow(2, attempt) * 1000;
      await sleep(backoffMs);
    }
  }
}
```

### Delivery Status Tracking

```typescript
interface NotificationDelivery {
  notificationId: string;
  channel: NotificationChannel;
  recipientId: string;
  templateId: string;
  
  // Delivery tracking
  status: DeliveryStatus; // PENDING, SENDING, SENT, DELIVERED, FAILED, BOUNCED
  attempts: number;
  maxAttempts: number;
  
  // Timestamps
  createdAt: DateTime;
  sentAt?: DateTime;
  deliveredAt?: DateTime;
  failedAt?: DateTime;
  nextRetryAt?: DateTime;
  
  // Provider tracking
  providerMessageId?: string;
  providerResponse?: ProviderResponse;
  
  // Error handling
  failureReason?: string;
  errorCode?: string;
  
  // Content (sanitized for logging)
  templateData?: Record<string, any>;
}

// Delivery status transitions
const DELIVERY_STATUS_TRANSITIONS = {
  PENDING: ['SENDING', 'FAILED'],
  SENDING: ['SENT', 'FAILED'],
  SENT: ['DELIVERED', 'FAILED', 'BOUNCED'],
  DELIVERED: [], // Final state
  FAILED: ['SENDING'], // Can retry
  BOUNCED: [] // Final state
};
```

---

## Reconciliation Process

### Reconciliation Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Reconciliation Service                       │
│                                                                     │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│   │   Internal   │  │   Provider   │  │   Settlement  │          │
│   │  Transaction │  │  Statement   │  │     File      │          │
│   │    Reader    │  │    Fetcher   │  │    Parser     │          │
│   └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│          │                 │                 │                    │
│          └─────────────────┴─────────────────┘                    │
│                            │                                       │
│                   ┌────────▼────────┐                              │
│                   │  Matching       │                              │
│                   │  Engine         │                              │
│                   └────────┬────────┘                              │
│                            │                                       │
│         ┌──────────────────┴──────────────────┐                  │
│         │                                          │                  │
│  ┌──────▼──────┐                          ┌───────▼──────┐         │
│  │  Matched   │                          │Discrepancies│         │
│  └──────┬──────┘                          └───────┬──────┘         │
│         │                                         │                  │
│         │                                         │                  │
│  ┌──────▼──────┐                          ┌───────▼──────┐         │
│  │  Verified  │                          │  Resolution │         │
│  │  Balances  │                          │  Workflow   │         │
│  └────────────┘                          └──────────────┘         │
└─────────────────────────────────────────────────────────────────────┘
```

### Daily Reconciliation Workflow

```
┌──────────────────────────────────────────────────────────────────┐
│                      Reconciliation Timeline                      │
└──────────────────────────────────────────────────────────────────┘

00:00 UTC - Daily reconciliation trigger
    │
    ├─ Step 1: Fetch internal transactions (00:00-00:30)
    │   ├─ Query completed payments from previous day
    │   ├─ Fetch from Payment Processing Service
    │   └─ Total records: ~50,000
    │
    ├─ Step 2: Fetch provider statements (00:00-01:00)
    │   ├─ Pull from all enabled providers (Stripe, Adyen, Banks)
    │   ├─ Download statement files via SFTP/API
    │   ├─ Parse and normalize transaction records
    │   └─ Total records: ~50,200 (some fees/adjustments)
    │
    ├─ Step 3: Execute matching algorithm (01:00-02:30)
    │   ├─ Exact match: transaction_id + amount + date
    │   ├─ Fuzzy match: amount + date + customer_id
    │   ├─ Confidence scoring for each match
    │   └─ Results:
    │       ├─ Matched: 49,950 (99.9%)
    │       ├─ Unmatched internal: 30
    │       └─ Unmatched provider: 220
    │
    ├─ Step 4: Discrepancy detection (02:30-03:00)
    │   ├─ Missing in internal: Provider has record, we don't
    │   ├─ Missing in provider: We have record, provider doesn't
    │   ├─ Amount mismatch: Same transaction, different amounts
    │   ├─ Status mismatch: We show completed, provider shows pending
    │   └─ Timing mismatch: Transaction dated differently
    │
    ├─ Step 5: Auto-resolution (03:00-03:30)
    │   ├─ Timing differences < 1 hour: Auto-match with warning
    │   ├─ Currency rounding differences: Auto-adjust
    │   ├─ Known fee structures: Auto-reconcile
    │   └─ Resolved: 200 of 250 discrepancies
    │
    ├─ Step 6: Manual investigation queue (03:30-04:00)
    │   ├─ Create 50 discrepancy tickets
    │   ├─ Assign to reconciliation analysts
    │   ├─ Prioritize by severity and amount
    │   └─ SLA: Resolve within 7 business days
    │
    ├─ Step 7: Settlement verification (04:00-05:00)
    │   ├─ Verify settlement batch totals
    │   ├─ Match settlement to transaction totals
    │   ├─ Calculate expected vs actual net amount
    │   └─ Flag settlement discrepancies
    │
    ├─ Step 8: Report generation (05:00-06:00)
    │   ├─ Generate daily reconciliation report
    │   ├─ Calculate match rates and metrics
    │   ├─ Create summary for finance team
    │   └─ Send alerts for critical issues
    │
    └─ Step 9: Complete (06:00)
        ├─ Mark reconciliation run as COMPLETED
        ├─ Publish ReconciliationCompleted event
        ├─ Archive run data
        └─ Schedule next day's run
```

### Matching Algorithm

```typescript
interface MatchingConfig {
  strategies: MatchingStrategy[];
  confidenceThreshold: number;
  autoResolveThreshold: number;
}

interface MatchingStrategy {
  name: string;
  priority: number;
  matcher: (internal: Transaction, provider: ProviderRecord) => MatchResult;
  weight: number;
}

async function executeMatching(
  internalTransactions: InternalTransaction[],
  providerRecords: ProviderRecord[],
  config: MatchingConfig
): Promise<MatchingResult> {
  
  const matches: Match[] = [];
  const unmatchedInternal: InternalTransaction[] = [];
  const unmatchedProvider: ProviderRecord[] = [];
  const discrepancies: Discrepancy[] = [];
  
  // Sort strategies by priority
  const sortedStrategies = config.strategies.sort((a, b) => b.priority - a.priority);
  
  for (const internalTx of internalTransactions) {
    let bestMatch: Match | null = null;
    let bestScore = 0;
    
    // Try each strategy in priority order
    for (const strategy of sortedStrategies) {
      const candidates = providerRecords.filter(record => 
        isPotentialMatch(internalTx, record, strategy)
      );
      
      for (const candidate of candidates) {
        const matchResult = await strategy.matcher(internalTx, candidate);
        
        if (matchResult.isMatch && matchResult.confidence > bestScore) {
          bestScore = matchResult.confidence;
          bestMatch = {
            internalTransactionId: internalTx.transactionId,
            providerTransactionId: candidate.transactionId,
            confidence: matchResult.confidence,
            strategy: strategy.name,
            matchDate: new Date()
          };
        }
      }
      
      // If we found a high-confidence match, stop trying other strategies
      if (bestMatch && bestScore >= config.confidenceThreshold) {
        break;
      }
    }
    
    if (bestMatch && bestScore >= config.autoResolveThreshold) {
      matches.push(bestMatch);
      
      // Remove matched provider record from pool
      const matchedIndex = providerRecords.findIndex(
        r => r.transactionId === bestMatch.providerTransactionId
      );
      if (matchedIndex >= 0) {
        providerRecords.splice(matchedIndex, 1);
      }
    } else if (bestMatch && bestScore >= config.confidenceThreshold) {
      // Low confidence match - create discrepancy for review
      discrepancies.push(createDiscrepancy(bestMatch));
    } else {
      // No match found
      unmatchedInternal.push(internalTx);
    }
  }
  
  // Remaining provider records are unmatched
  unmatchedProvider.push(...providerRecords);
  
  return {
    matches,
    unmatchedInternal,
    unmatchedProvider,
    discrepancies,
    totalInternal: internalTransactions.length,
    totalProvider: internalTransactions.length + providerRecords.length,
    matchRate: matches.length / internalTransactions.length
  };
}

// Exact match strategy
const exactMatchStrategy: MatchingStrategy = {
  name: 'EXACT_MATCH',
  priority: 100,
  weight: 1.0,
  matcher: (internal, provider) => {
    if (internal.transactionId === provider.transactionId &&
        Math.abs(internal.amount.value - provider.amount.value) < 0.01 &&
        isSameDay(internal.transactionDate, provider.transactionDate)) {
      return {
        isMatch: true,
        confidence: 1.0,
        reason: 'Exact match on transaction ID, amount, and date'
      };
    }
    
    return { isMatch: false, confidence: 0 };
  }
};

// Fuzzy match strategy
const fuzzyMatchStrategy: MatchingStrategy = {
  name: 'FUZZY_MATCH',
  priority: 50,
  weight: 0.7,
  matcher: async (internal, provider) => {
    let score = 0;
    const reasons: string[] = [];
    
    // Amount match (highest weight)
    if (Math.abs(internal.amount.value - provider.amount.value) < 0.01) {
      score += 0.5;
      reasons.push('Amount matches');
    }
    
    // Date match (allowing 1 day variance for timezone differences)
    const dayDiff = Math.abs(
      internal.transactionDate.getTime() - provider.transactionDate.getTime()
    ) / (1000 * 60 * 60 * 24);
    
    if (dayDiff <= 1) {
      score += 0.3;
      reasons.push('Date within 1 day');
    }
    
    // Customer match
    if (internal.customerId === provider.customerId) {
      score += 0.2;
      reasons.push('Customer ID matches');
    }
    
    // Confidence threshold
    if (score >= 0.7) {
      return {
        isMatch: true,
        confidence: score,
        reason: reasons.join(', ')
      };
    }
    
    return { isMatch: false, confidence: score };
  }
};
```

### Discrepancy Handling

```typescript
enum DiscrepancyType {
  MISSING_IN_INTERNAL = 'MISSING_IN_INTERNAL',     // Provider has, we don't
  MISSING_IN_PROVIDER = 'MISSING_IN_PROVIDER',     // We have, provider doesn't
  AMOUNT_MISMATCH = 'AMOUNT_MISMATCH',             // Same tx, different amount
  STATUS_MISMATCH = 'STATUS_MISMATCH',             // Different statuses
  TIMING_MISMATCH = 'TIMING_MISMATCH',             // Date discrepancies
  CURRENCY_MISMATCH = 'CURRENCY_MISMATCH',         // Currency differences
  DUPLICATE_TRANSACTION = 'DUPLICATE_TRANSACTION'    // Possible duplicate
}

enum DiscrepancyStatus {
  OPEN = 'OPEN',
  INVESTIGATING = 'INVESTIGATING',
  RESOLVED = 'RESOLVED',
  IGNORED = 'IGNORED'
}

enum DiscrepancySeverity {
  LOW = 'LOW',       // <$10, timing differences
  MEDIUM = 'MEDIUM', // $10-$1000, status mismatches
  HIGH = 'HIGH',     // >$1000, amount mismatches
  CRITICAL = 'CRITICAL' // >$10000, missing transactions
}

interface Discrepancy {
  discrepancyId: string;
  runId: string;
  type: DiscrepancyType;
  severity: DiscrepancySeverity;
  
  // Transaction references
  internalTransactionId?: string;
  providerTransactionId?: string;
  
  // Details
  internalAmount?: Money;
  providerAmount?: Money;
  difference?: Money;
  
  // Status and resolution
  status: DiscrepancyStatus;
  resolution?: string;
  resolvedBy?: string;
  resolvedAt?: DateTime;
  
  // Assignment
  assignedTo?: string;
  priority: number;
  
  // Audit
  createdAt: DateTime;
  updatedAt: DateTime;
  comments: Comment[];
}

async function handleDiscrepancy(
  discrepancy: Discrepancy,
  action: ResolutionAction
): Promise<void> {
  
  switch (action.type) {
    case 'AUTO_RESOLVE':
      // System can auto-resolve based on business rules
      await autoResolveDiscrepancy(discrepancy, action);
      break;
      
    case 'MANUAL_INVESTIGATE':
      // Assign to analyst for investigation
      await assignToAnalyst(discrepancy, action.analystId);
      break;
      
    case 'IGNORE':
      // Legitimate difference (e.g., known fee structure)
      await ignoreDiscrepancy(discrepancy, action.reason);
      break;
      
    case 'ESCALATE':
      // Requires finance team or management review
      await escalateDiscrepancy(discrepancy, action.escalationLevel);
      break;
  }
}

async function autoResolveDiscrepancy(
  discrepancy: Discrepancy,
  action: ResolutionAction
): Promise<void> {
  
  // Apply business rules for auto-resolution
  switch (discrepancy.type) {
    case DiscrepancyType.TIMING_MISMATCH:
      // Allow 1-day variance for timezone differences
      if (discrepancy.difference.value < 24 * 60 * 60 * 1000) {
        await markAsResolved(discrepancy, {
          resolution: 'Timing difference within acceptable tolerance (24h)',
          autoResolved: true
        });
      }
      break;
      
    case DiscrepancyType.AMOUNT_MISMATCH:
      // Small currency rounding differences
      if (Math.abs(discrepancy.difference.value) < 0.02) {
        await markAsResolved(discrepancy, {
          resolution: 'Currency rounding difference within tolerance',
          autoResolved: true
        });
      }
      break;
      
    case DiscrepancyType.STATUS_MISMATCH:
      // Provider lag in status updates
      if (action.confirmedWithProvider) {
        await markAsResolved(discrepancy, {
          resolution: 'Status confirmed with provider - update pending',
          autoResolved: true
        });
      }
      break;
  }
}

async function assignToAnalyst(
  discrepancy: Discrepancy,
  analystId: string
): Promise<void> {
  
  await discrepancyRepository.update(discrepancy.discrepancyId, {
    status: DiscrepancyStatus.INVESTIGATING,
    assignedTo: analystId,
    updatedAt: new Date()
  });
  
  // Send notification to analyst
  await notificationService.send({
    recipientId: analystId,
    templateId: 'discrepancy_assigned',
    templateData: {
      discrepancyId: discrepancy.discrepancyId,
      type: discrepancy.type,
      severity: discrepancy.severity,
      amount: discrepancy.difference?.value || 0
    },
    priority: discrepancy.severity === DiscrepancySeverity.CRITICAL ? 'HIGH' : 'NORMAL'
  });
  
  // Create ticket in ticketing system
  await ticketingService.createTicket({
    title: `Reconciliation Discrepancy: ${discrepancy.type}`,
    description: `Discrepancy ${discrepancy.discrepancyId} requires investigation`,
    priority: discrepancy.severity,
    assignee: analystId,
    dueDate: calculateDueDate(discrepancy.severity)
  });
}
```

### Settlement Reconciliation

```typescript
interface Settlement {
  settlementId: string;
  providerId: string;
  settlementDate: Date;
  settlementBatchId: string;
  
  // Financial details
  grossAmount: Money;
  feeAmount: Money;
  netAmount: Money;
  transactionCount: number;
  currency: string;
  
  // Reconciliation status
  status: SettlementStatus;
  reconciledAt?: DateTime;
  discrepancies: SettlementDiscrepancy[];
  
  // Audit
  receivedAt: DateTime;
  createdAt: DateTime;
}

enum SettlementStatus {
  PENDING = 'PENDING',
  RECEIVED = 'RECEIVED',
  VERIFIED = 'VERIFIED',
  DISCREPANCY = 'DISCREPANCY'
}

async function reconcileSettlement(
  settlement: Settlement
): Promise<SettlementReconciliationResult> {
  
  // Fetch all transactions for this settlement period
  const transactions = await paymentRepository.findBySettlementBatch(
    settlement.providerId,
    settlement.settlementDate
  );
  
  // Calculate expected totals
  const expectedGross = transactions.reduce((sum, tx) => 
    sum + tx.amount.value, 0
  );
  
  const expectedFee = calculateExpectedFees(transactions, settlement.providerId);
  const expectedNet = expectedGross - expectedFee;
  const expectedCount = transactions.length;
  
  // Compare with actual settlement
  const grossDiff = Math.abs(expectedGross - settlement.grossAmount.value);
  const feeDiff = Math.abs(expectedFee - settlement.feeAmount.value);
  const netDiff = Math.abs(expectedNet - settlement.netAmount.value);
  const countDiff = expectedCount - settlement.transactionCount;
  
  const discrepancies: SettlementDiscrepancy[] = [];
  
  if (grossDiff > 0.01) {
    discrepancies.push({
      type: 'GROSS_AMOUNT_MISMATCH',
      expected: expectedGross,
      actual: settlement.grossAmount.value,
      difference: grossDiff
    });
  }
  
  if (feeDiff > 0.01) {
    discrepancies.push({
      type: 'FEE_AMOUNT_MISMATCH',
      expected: expectedFee,
      actual: settlement.feeAmount.value,
      difference: feeDiff
    });
  }
  
  if (netDiff > 0.01) {
    discrepancies.push({
      type: 'NET_AMOUNT_MISMATCH',
      expected: expectedNet,
      actual: settlement.netAmount.value,
      difference: netDiff
    });
  }
  
  if (countDiff !== 0) {
    discrepancies.push({
      type: 'TRANSACTION_COUNT_MISMATCH',
      expected: expectedCount,
      actual: settlement.transactionCount,
      difference: countDiff
    });
  }
  
  // Update settlement status
  if (discrepancies.length === 0) {
    await settlementRepository.update(settlement.settlementId, {
      status: SettlementStatus.VERIFIED,
      reconciledAt: new Date()
    });
    
    return {
      status: 'VERIFIED',
      discrepancies: [],
      summary: 'Settlement reconciled successfully'
    };
  } else {
    await settlementRepository.update(settlement.settlementId, {
      status: SettlementStatus.DISCREPANCY,
      discrepancies
    });
    
    return {
      status: 'DISCREPANCY',
      discrepancies,
      summary: `${discrepancies.length} discrepancies found in settlement`
    };
  }
}
```

### Reconciliation Reporting

```typescript
interface ReconciliationReport {
  runId: string;
  periodStart: DateTime;
  periodEnd: DateTime;
  
  // Summary metrics
  totalInternalTransactions: number;
  totalProviderTransactions: number;
  matchedTransactions: number;
  matchRate: number;
  
  // Discrepancy summary
  totalDiscrepancies: number;
  discrepanciesByType: Record<DiscrepancyType, number>;
  discrepanciesBySeverity: Record<DiscrepancySeverity, number>;
  resolvedDiscrepancies: number;
  pendingDiscrepancies: number;
  
  // Settlement summary
  settlementsProcessed: number;
  settlementsVerified: number;
  settlementsWithDiscrepancy: number;
  
  // Financial summary
  totalInternalAmount: Money;
  totalProviderAmount: Money;
  totalDiscrepancyAmount: Money;
  
  // Performance metrics
  processingStartTime: DateTime;
  processingEndTime: DateTime;
  totalDuration: number;
  
  // SLA compliance
  slaCompliant: boolean;
  slaNotes?: string;
}

async function generateReconciliationReport(
  run: ReconciliationRun
): Promise<ReconciliationReport> {
  
  // Fetch all data for the run
  const matches = await matchRepository.findByRunId(run.runId);
  const discrepancies = await discrepancyRepository.findByRunId(run.runId);
  const settlements = await settlementRepository.findByRunId(run.runId);
  
  // Calculate metrics
  const totalInternal = run.summary.totalInternalTransactions;
  const totalProvider = run.summary.totalProviderTransactions;
  const matched = matches.length;
  const matchRate = (matched / totalInternal) * 100;
  
  // Discrepancy breakdown
  const discrepanciesByType = groupBy(discrepancies, 'type');
  const discrepanciesBySeverity = groupBy(discrepancies, 'severity');
  const resolved = discrepancies.filter(d => d.status === DiscrepancyStatus.RESOLVED).length;
  const pending = discrepancies.filter(d => d.status === DiscrepancyStatus.OPEN).length;
  
  // Settlement breakdown
  const settlementsVerified = settlements.filter(s => s.status === SettlementStatus.VERIFIED).length;
  const settlementsWithDiscrepancy = settlements.filter(s => s.status === SettlementStatus.DISCREPANCY).length;
  
  // Financial summary
  const internalAmount = await calculateTotalInternalAmount(run.periodStart, run.periodEnd);
  const providerAmount = await calculateTotalProviderAmount(run.periodStart, run.periodEnd);
  const discrepancyAmount = await calculateTotalDiscrepancyAmount(discrepancies);
  
  // SLA check
  const slaCompliant = matchRate >= 99.9 && run.completedAt;
  
  const report: ReconciliationReport = {
    runId: run.runId,
    periodStart: run.periodStart,
    periodEnd: run.periodEnd,
    
    totalInternalTransactions: totalInternal,
    totalProviderTransactions: totalProvider,
    matchedTransactions: matched,
    matchRate: Math.round(matchRate * 100) / 100,
    
    totalDiscrepancies: discrepancies.length,
    discrepanciesByType,
    discrepanciesBySeverity,
    resolvedDiscrepancies: resolved,
    pendingDiscrepancies: pending,
    
    settlementsProcessed: settlements.length,
    settlementsVerified: settlementsVerified,
    settlementsWithDiscrepancy,
    
    totalInternalAmount: internalAmount,
    totalProviderAmount: providerAmount,
    totalDiscrepancyAmount: discrepancyAmount,
    
    processingStartTime: run.startedAt,
    processingEndTime: run.completedAt || new Date(),
    totalDuration: run.completedAt ? 
      run.completedAt.getTime() - run.startedAt.getTime() : 0,
    
    slaCompliant,
    slaNotes: slaCompliant ? undefined : 
      `Match rate ${matchRate}% below SLA threshold of 99.9%`
  };
  
  // Store report
  await reportRepository.create(report);
  
  // Send to finance team
  await notificationService.send({
    recipientId: 'finance-team',
    templateId: 'daily_reconciliation_report',
    templateData: report,
    channels: ['EMAIL']
  });
  
  return report;
}
```

### Chargeback Processing

```typescript
interface Chargeback {
  chargebackId: string;
  providerId: string;
  paymentId: string;
  chargebackAmount: Money;
  chargebackReason: string;
  chargebackDate: Date;
  
  // Status tracking
  status: ChargebackStatus;
  responseDeadline: DateTime;
  responseEvidence?: string;
  respondedAt?: DateTime;
  
  // Outcome
  outcome?: ChargebackOutcome;
  lossAmount?: Money;
  
  // Audit
  createdAt: DateTime;
  updatedAt: DateTime;
}

enum ChargebackStatus {
  PENDING = 'PENDING',
  RESPONDED = 'RESPONDED',
  WON = 'WON',
  LOST = 'LOST',
  EXPIRED = 'EXPIRED'
}

enum ChargebackOutcome {
  FULL_REVERSAL = 'FULL_REVERSAL',
  PARTIAL_REVERSAL = 'PARTIAL_REVERSAL',
  WON = 'WON'
}

async function processChargeback(
  chargebackNotification: ChargebackNotification
): Promise<void> {
  
  // Create chargeback record
  const chargeback = await chargebackRepository.create({
    chargebackId: generateId(),
    providerId: chargebackNotification.providerId,
    paymentId: chargebackNotification.paymentId,
    chargebackAmount: chargebackNotification.amount,
    chargebackReason: chargebackNotification.reason,
    chargebackDate: chargebackNotification.date,
    status: ChargebackStatus.PENDING,
    responseDeadline: calculateResponseDeadline(chargebackNotification.date),
    createdAt: new Date()
  });
  
  // Publish event
  await eventBus.publish('ChargebackReceived', {
    chargebackId: chargeback.chargebackId,
    paymentId: chargeback.paymentId,
    amount: chargeback.chargebackAmount,
    deadline: chargeback.responseDeadline
  });
  
  // Alert payment and ledger services
  await paymentService.markForChargeback(chargeback.paymentId);
  await ledgerService.createChargebackReserve(chargeback);
  
  // Notify operations team
  await notificationService.send({
    recipientId: 'operations-team',
    templateId: 'chargeback_alert',
    templateData: {
      chargebackId: chargeback.chargebackId,
      amount: chargeback.chargebackAmount,
      deadline: chargeback.responseDeadline,
      reason: chargeback.chargebackReason
    },
    priority: 'HIGH'
  });
}

async function submitChargebackResponse(
  chargebackId: string,
  evidence: ChargebackEvidence
): Promise<void> {
  
  const chargeback = await chargebackRepository.findById(chargebackId);
  
  if (chargeback.status !== ChargebackStatus.PENDING) {
    throw new Error(`Cannot respond to chargeback in status ${chargeback.status}`);
  }
  
  // Check deadline
  if (new Date() > chargeback.responseDeadline) {
    await chargebackRepository.update(chargebackId, {
      status: ChargebackStatus.EXPIRED,
      updatedAt: new Date()
    });
    
    throw new Error('Chargeback response deadline has expired');
  }
  
  // Submit evidence to provider
  const response = await bankAdapterService.submitChargebackEvidence({
    providerId: chargeback.providerId,
    chargebackId: chargeback.providerChargebackId,
    evidence: evidence
  });
  
  // Update chargeback
  await chargebackRepository.update(chargebackId, {
    status: ChargebackStatus.RESPONDED,
    responseEvidence: JSON.stringify(evidence),
    respondedAt: new Date(),
    updatedAt: new Date()
  });
  
  // Publish event
  await eventBus.publish('ChargebackResponseSubmitted', {
    chargebackId,
    submittedAt: new Date()
  });
}

async function processChargebackOutcome(
  chargebackId: string,
  outcome: ChargebackOutcome
): Promise<void> {
  
  const chargeback = await chargebackRepository.findById(chargebackId);
  
  // Update chargeback
  await chargebackRepository.update(chargebackId, {
    status: outcome === ChargebackOutcome.WON ? ChargebackStatus.WON : ChargebackStatus.LOST,
    outcome,
    lossAmount: outcome === ChargebackOutcome.FULL_REVERSAL ? chargeback.chargebackAmount :
                outcome === ChargebackOutcome.PARTIAL_REVERSAL ? calculatePartialLoss(chargeback) : 
                { value: 0, currency: chargeback.chargebackAmount.currency },
    updatedAt: new Date()
  });
  
  // Create ledger entry for loss
  if (outcome !== ChargebackOutcome.WON) {
    await ledgerService.createChargebackLossEntry(chargeback);
  }
  
  // Publish event
  await eventBus.publish('ChargebackOutcomeReceived', {
    chargebackId,
    outcome,
    lossAmount: chargeback.lossAmount
  });
}
```

---

## Domain Entities

### Payment Processing Domain

```typescript
// Payment Aggregate
interface Payment {
  paymentId: string;
  merchantId: string;
  customerId: string;
  amount: Money;
  currency: string;
  status: PaymentStatus;
  idempotencyKey: string;
  createdAt: DateTime;
  updatedAt: DateTime;
  completedAt?: DateTime;
  failureReason?: string;
  metadata: Record<string, any>;
}

enum PaymentStatus {
  CREATED = 'CREATED',
  PROCESSING = 'PROCESSING',
  RISK_EVALUATION = 'RISK_EVALUATION',
  ROUTING = 'ROUTING',
  AUTHORIZING = 'AUTHORIZING',
  COMPLETED = 'COMPLETED',
  FAILED = 'FAILED',
  CANCELLED = 'CANCELLED'
}

// Payment Attempt
interface PaymentAttempt {
  attemptId: string;
  paymentId: string;
  attemptNumber: number;
  startedAt: DateTime;
  completedAt?: DateTime;
  status: AttemptStatus;
  failureReason?: string;
  provider: string;
}

enum AttemptStatus {
  STARTED = 'STARTED',
  COMPLETED = 'COMPLETED',
  FAILED = 'FAILED'
}

// Money Value Object
interface Money {
  value: number;
  currency: string;
}
```

### Risk Assessment Domain

```typescript
// Risk Evaluation
interface RiskEvaluation {
  evaluationId: string;
  paymentId: string;
  merchantId: string;
  customerId: string;
  riskScore: number; // 0-100
  riskDecision: RiskDecision;
  evaluatedAt: DateTime;
  ruleVersion: string;
  triggeredRules: RuleTrigger[];
  evaluationDetails: EvaluationDetails;
}

enum RiskDecision {
  APPROVE = 'APPROVE',
  REJECT = 'REJECT',
  REVIEW = 'REVIEW'
}

interface EvaluationDetails {
  velocityChecks: VelocityCheckResult;
  blacklistCheck: boolean;
  whitelistCheck: boolean;
  geoLocationCheck: GeoLocationResult;
  amountCheck: AmountCheckResult;
  customerProfile: CustomerRiskProfile;
}

// Risk Rule
interface RiskRule {
  ruleId: string;
  ruleName: string;
  ruleType: RuleType;
  version: number;
  isActive: boolean;
  priority: number;
  conditions: RuleCondition[];
  action: RuleAction;
  scoreImpact: number;
  createdAt: DateTime;
  updatedAt: DateTime;
}

enum RuleType {
  VELOCITY = 'VELOCITY',
  BLACKLIST = 'BLACKLIST',
  AMOUNT = 'AMOUNT',
  GEO = 'GEO',
  ML_MODEL = 'ML_MODEL'
}

enum RuleAction {
  BLOCK = 'BLOCK',
  FLAG = 'FLAG',
  ADD_SCORE = 'ADD_SCORE'
}

// Merchant Risk Profile
interface MerchantRiskProfile {
  profileId: string;
  merchantId: string;
  riskLevel: RiskLevel;
  riskThresholds: RiskThresholds;
  enabledRules: string[];
  whitelistedCountries: string[];
  blacklistedCountries: string[];
  maxTransactionAmount: Money;
  velocityLimits: VelocityLimit[];
}

enum RiskLevel {
  LOW = 'LOW',
  MEDIUM = 'MEDIUM',
  HIGH = 'HIGH'
}
```

### Payment Router Domain

```typescript
// Payment Provider
interface PaymentProvider {
  providerId: string;
  providerName: string;
  providerType: ProviderType;
  supportedCurrencies: string[];
  supportedMethods: PaymentMethod[];
  priority: number;
  isEnabled: boolean;
  costConfig: CostConfig;
  performanceMetrics: PerformanceMetrics;
  healthStatus: HealthStatus;
  circuitBreakerState: CircuitState;
}

enum ProviderType {
  BANK = 'BANK',
  PAYMENT_GATEWAY = 'PAYMENT_GATEWAY',
  WALLET = 'WALLET'
}

enum HealthStatus {
  HEALTHY = 'HEALTHY',
  DEGRADED = 'DEGRADED',
  UNHEALTHY = 'UNHEALTHY'
}

enum CircuitState {
  CLOSED = 'CLOSED',
  OPEN = 'OPEN',
  HALF_OPEN = 'HALF_OPEN'
}

interface CostConfig {
  fixedFee: Money;
  percentageFee: number;
  minFee?: Money;
  maxFee?: Money;
}

// Routing Decision
interface RoutingDecision {
  decisionId: string;
  paymentId: string;
  selectedProviderId: string;
  alternativeProviderIds: string[];
  routingStrategy: string;
  decisionReason: string;
  costEstimate: Money;
  decisionMadeAt: DateTime;
}
```

### Ledger Domain

```typescript
// Account
interface Account {
  accountId: string;
  accountNumber: string;
  accountType: AccountType;
  accountSubtype: string;
  ownerId: string;
  currency: string;
  currentBalance: Money;
  availableBalance: Money;
  isOverdraftAllowed: boolean;
  overdraftLimit?: Money;
  isActive: boolean;
  createdAt: DateTime;
  updatedAt: DateTime;
}

enum AccountType {
  ASSET = 'ASSET',
  LIABILITY = 'LIABILITY',
  EQUITY = 'EQUITY',
  REVENUE = 'REVENUE',
  EXPENSE = 'EXPENSE'
}

// Journal Entry
interface JournalEntry {
  entryId: string;
  entryDate: DateTime;
  entryNumber: string;
  referenceType: ReferenceType;
  referenceId: string;
  description: string;
  status: EntryStatus;
  totalDebit: Money;
  totalCredit: Money;
  lines: JournalEntryLine[];
  postedAt?: DateTime;
  createdBy: string;
  createdAt: DateTime;
  updatedAt: DateTime;
}

enum ReferenceType {
  PAYMENT = 'PAYMENT',
  REFUND = 'REFUND',
  CHARGEBACK = 'CHARGEBACK',
  ADJUSTMENT = 'ADJUSTMENT'
}

enum EntryStatus {
  PENDING = 'PENDING',
  POSTED = 'POSTED',
  REVERSED = 'REVERSED'
}

// Journal Entry Line
interface JournalEntryLine {
  lineId: string;
  entryId: string;
  accountId: string;
  debitAmount: Money;
  creditAmount: Money;
  description: string;
  lineSequence: number;
}
```

### Reconciliation Domain

```typescript
// Reconciliation Run
interface ReconciliationRun {
  runId: string;
  runType: RunType;
  providerId: string;
  periodStart: DateTime;
  periodEnd: DateTime;
  status: ReconciliationStatus;
  summary: ReconciliationSummary;
  startedAt: DateTime;
  completedAt?: DateTime;
  error?: string;
}

enum RunType {
  DAILY = 'DAILY',
  AD_HOC = 'AD_HOC',
  PROVIDER_SPECIFIC = 'PROVIDER_SPECIFIC'
}

enum ReconciliationStatus {
  RUNNING = 'RUNNING',
  COMPLETED = 'COMPLETED',
  FAILED = 'FAILED',
  PARTIALLY_COMPLETED = 'PARTIALLY_COMPLETED'
}

interface ReconciliationSummary {
  totalInternalTransactions: number;
  totalProviderTransactions: number;
  matchedTransactions: number;
  unmatchedInternalTransactions: number;
  unmatchedProviderTransactions: number;
  discrepancies: number;
}

// Discrepancy
interface Discrepancy {
  discrepancyId: string;
  runId: string;
  discrepancyType: DiscrepancyType;
  internalTransactionId?: string;
  providerTransactionId?: string;
  internalAmount?: Money;
  providerAmount?: Money;
  difference?: Money;
  status: DiscrepancyStatus;
  resolution?: string;
  resolvedBy?: string;
  resolvedAt?: DateTime;
  createdAt: DateTime;
  updatedAt: DateTime;
}

enum DiscrepancyType {
  MISSING_IN_INTERNAL = 'MISSING_IN_INTERNAL',
  MISSING_IN_PROVIDER = 'MISSING_IN_PROVIDER',
  AMOUNT_MISMATCH = 'AMOUNT_MISMATCH',
  STATUS_MISMATCH = 'STATUS_MISMATCH',
  TIMING_MISMATCH = 'TIMING_MISMATCH'
}

enum DiscrepancyStatus {
  OPEN = 'OPEN',
  INVESTIGATING = 'INVESTIGATING',
  RESOLVED = 'RESOLVED',
  IGNORED = 'IGNORED'
}

// Settlement
interface Settlement {
  settlementId: string;
  providerId: string;
  settlementDate: Date;
  settlementBatchId: string;
  totalAmount: Money;
  transactionCount: number;
  feeAmount: Money;
  netAmount: Money;
  currency: string;
  status: SettlementStatus;
  verifiedAt?: DateTime;
  discrepancies: string[];
  receivedAt: DateTime;
}

enum SettlementStatus {
  PENDING = 'PENDING',
  RECEIVED = 'RECEIVED',
  VERIFIED = 'VERIFIED',
  DISCREPANCY = 'DISCREPANCY'
}

// Chargeback
interface Chargeback {
  chargebackId: string;
  providerId: string;
  paymentId: string;
  chargebackAmount: Money;
  chargebackReason: string;
  chargebackDate: Date;
  status: ChargebackStatus;
  responseDeadline: DateTime;
  responseEvidence?: string;
  respondedAt?: DateTime;
  outcome?: ChargebackOutcome;
  createdAt: DateTime;
  updatedAt: DateTime;
}

enum ChargebackStatus {
  PENDING = 'PENDING',
  RESPONDED = 'RESPONDED',
  WON = 'WON',
  LOST = 'LOST',
  EXPIRED = 'EXPIRED'
}

enum ChargebackOutcome {
  FULL_REVERSAL = 'FULL_REVERSAL',
  PARTIAL_REVERSAL = 'PARTIAL_REVERSAL',
  WON = 'WON'
}
```

---

## Service Responsibilities

### API Gateway Service

| Responsibility | Description | Implementation Details |
|----------------|-------------|------------------------|
| **Authentication** | Validate OAuth2/OIDC tokens | Bearer token validation, identity provider integration |
| **Authorization** | Extract merchant/user context | Scope validation, merchant ID extraction |
| **Rate Limiting** | Prevent abuse | Token bucket algorithm, per-merchant limits |
| **Request Routing** | Route to appropriate service | Path-based routing, service discovery |
| **Request Validation** | Schema validation | JSON schema validation, size limits |
| **Circuit Breaking** | Prevent cascading failures | Per-service circuit breakers |
| **Observability** | Add tracing context | Trace ID injection, logging |

### Payment Processing Service

| Responsibility | Description | Implementation Details |
|----------------|-------------|------------------------|
| **Payment Creation** | Create payment records | Idempotency checks, state initialization |
| **State Machine** | Orchestrate payment flow | State transitions, validation, audit logging |
| **Saga Orchestration** | Coordinate distributed transactions | Compensating transactions, event coordination |
| **Idempotency** | Prevent duplicate payments | Idempotency key checks, 24-hour TTL |
| **Retry Logic** | Handle transient failures | Exponential backoff, max 3 attempts |
| **Cancellation** | Cancel pending payments | State validation, downstream notification |

### Risk Assessment Service

| Responsibility | Description | Implementation Details |
|----------------|-------------|------------------------|
| **Real-time Evaluation** | Score payment risk | Rule engine, ML model integration |
| **Velocity Checking** | Detect unusual patterns | Multiple time windows, Redis-backed |
| **Blacklist/Whitelist** | Customer-based filtering | Database-backed, cached |
| **Geo Validation** | Location-based checks | IP geolocation, country validation |
| **Merchant Profiles** | Merchant-specific rules | Configurable thresholds, isolated |
| **Risk Scoring** | Calculate 0-100 score | Weighted rule evaluation, ML model |
| **Determinism** | Consistent results | Same input → same output |

### Payment Router Service

| Responsibility | Description | Implementation Details |
|----------------|-------------|------------------------|
| **Provider Selection** | Choose optimal provider | Cost-based, performance-based, priority-based |
| **Cost Optimization** | Minimize processing costs | Fixed + percentage fee calculation |
| **Circuit Breaking** | Avoid unhealthy providers | Per-provider circuit breakers |
| **Health Checking** | Monitor provider status | Periodic health checks, status updates |
| **Performance Tracking** | Measure provider metrics | Success rate, latency, volume |
| **Fallback Logic** | Provide alternatives | Multiple provider options |

### Bank Adapter Service

| Responsibility | Description | Implementation Details |
|----------------|-------------|------------------------|
| **Provider Integration** | Normalize provider APIs | Translation layer, protocol handling |
| **Retry Logic** | Handle transient failures | Provider-specific retry config |
| **Rate Limiting** | Respect provider limits | Per-provider rate limiting |
| **Error Handling** | Normalize errors | Error classification, sanitization |
| **Request Logging** | Maintain request history | Sanitized logging, PCI-DSS compliance |
| **Credential Management** | Secure provider auth | Encrypted storage, vault integration |

### Ledger Service

| Responsibility | Description | Implementation Details |
|----------------|-------------|------------------------|
| **Double-Entry Bookkeeping** | Balanced journal entries | Debit = credit enforcement |
| **Account Management** | Track balances | Current + available balances |
| **Journal Posting** | Record transactions | Atomic posting, audit trail |
| **Balance Updates** | Maintain account balances | Transactional updates |
| **Reconciliation Support** | Provide reconciliation data | Period queries, transaction matching |
| **Audit Trail** | Track all changes | Immutable entries, user tracking |

### Notification Service

| Responsibility | Description | Implementation Details |
|----------------|-------------|------------------------|
| **Multi-channel Delivery** | Email, SMS, push, webhooks | Provider integration, delivery tracking |
| **Template Management** | Render personalized messages | Multi-language, variable substitution |
| **Preference Management** | Respect user choices | Per-channel opt-out, category filtering |
| **Delivery Tracking** | Monitor delivery status | Provider callbacks, receipt handling |
| **Retry Logic** | Handle delivery failures | Exponential backoff, max attempts |
| **Rate Limiting** | Respect provider limits | Per-channel rate limiting |

### Reconciliation Service

| Responsibility | Description | Implementation Details |
|----------------|-------------|------------------------|
| **Daily Reconciliation** | Match transactions | Automated daily runs, provider-wide |
| **Matching Engine** | Find transaction matches | Exact + fuzzy matching, confidence scoring |
| **Discrepancy Detection** | Identify differences | Type classification, severity assignment |
| **Settlement Processing** | Verify settlement batches | Batch verification, amount validation |
| **Chargeback Processing** | Handle chargebacks | Response submission, deadline tracking |
| **Report Generation** | Create reconciliation reports | Daily summaries, discrepancy tracking |

---

## Business Rules

### Payment Processing Rules

| Rule ID | Description | Priority | Error Handling |
|---------|-------------|----------|----------------|
| **BR-PP-001** | Payment amount cannot be negative | P0 | Reject with 400 Bad Request |
| **BR-PP-002** | Currency must be supported | P0 | Reject with 400 Bad Request |
| **BR-PP-003** | Idempotency keys must be unique per merchant | P0 | Return existing payment if duplicate |
| **BR-PP-004** | Payment must pass risk check before routing | P0 | Reject if risk score > 80 |
| **BR-PP-005** | Completed payment cannot be modified | P0 | Reject modification attempts |
| **BR-PP-006** | Payment can only transition to valid next states | P0 | Reject invalid transitions |
| **BR-PP-007** | Failed payments after max retries cannot be retried | P0 | Mark as permanently failed |
| **BR-PP-008** | Cancellation only allowed before COMPLETED state | P0 | Reject cancellation if completed |

### Risk Assessment Rules

| Rule ID | Description | Threshold | Action |
|---------|-------------|-----------|--------|
| **BR-RS-001** | High-risk transactions must be rejected | Score > 80 | REJECT |
| **BR-RS-002** | Medium-risk transactions require manual review | Score 50-80 | REVIEW |
| **BR-RS-003** | Low-risk transactions are auto-approved | Score < 50 | APPROVE |
| **BR-RS-004** | Blacklisted customers always rejected | In blacklist | REJECT |
| **BR-RS-005** | Whitelisted customers bypass velocity checks | In whitelist | SKIP_VELOCITY |
| **BR-RS-006** | Risk rules must be versioned and immutable | N/A | Version control |
| **BR-RS-007** | Evaluation timeout defaults to APPROVE with warning | Timeout > 500ms | APPROVE_WARN |

### Velocity Rules

| Window | Max Transactions | Action |
|--------|------------------|--------|
| **1 Minute** | 3 | Flag as suspicious |
| **5 Minutes** | 10 | Flag as suspicious |
| **1 Hour** | 30 | Flag as suspicious |
| **1 Day** | 100 | Flag as suspicious |

### Payment Routing Rules

| Rule ID | Description | Priority |
|---------|-------------|----------|
| **BR-PR-001** | Must route to healthy providers only | P0 |
| **BR-PR-002** | Must respect merchant provider preferences | P1 |
| **BR-PR-003** | Must respect currency and payment method constraints | P0 |
| **BR-PR-004** | Must have at least one fallback provider | P0 |
| **BR-PR-005** | Circuit breaker opens after 5 consecutive failures | P0 |
| **BR-PR-006** | Routing must be deterministic (same input → same provider) | P1 |

### Ledger Rules

| Rule ID | Description | Priority |
|---------|-------------|----------|
| **BR-LG-001** | Every transaction must balance (debits = credits) | P0 |
| **BR-LG-002** | Account balances must never go negative unless overdraft enabled | P0 |
| **BR-LG-003** | Posted transactions must be immutable | P0 |
| **BR-LG-004** | Every journal entry must have audit trail | P0 |
| **BR-LG-005** | Reconciliation must match external provider records | P1 |

### Reconciliation Rules

| Rule ID | Description | SLA | Action |
|---------|-------------|-----|--------|
| **BR-RC-001** | All transactions must be reconciled within 24 hours | 24 hours | Daily run |
| **BR-RC-002** | Discrepancies must be investigated and resolved | 7 days | Manual review |
| **BR-RC-003** | Reconciliation must balance (matches + discrepancies = total) | N/A | Validation check |
| **BR-RC-004** | Chargebacks must be processed within provider deadlines | Provider SLA | Escalation |
| **BR-RC-005** | Settlements must be tracked and verified | Daily | Settlement reconciliation |

### Notification Rules

| Rule ID | Description | Priority |
|---------|-------------|----------|
| **BR-NT-001** | Notifications must be delivered at-least-once | P0 |
| **BR-NT-002** | Failed notifications must be retried with backoff | P1 |
| **BR-NT-003** | User preferences must be respected | P0 |
| **BR-NT-004** | Templates must be validated before use | P0 |
| **BR-NT-005** | Rate limits must be enforced per channel | P0 |
| **BR-NT-006** | Notifications must not exceed size limits | P0 |

### SLA Requirements

| Metric | Target | Measurement | Alert Threshold |
|--------|--------|-------------|-----------------|
| **Payment Success Rate** | > 95% | Daily | < 93% |
| **Risk Evaluation Latency** | p95 < 500ms | Per request | > 1000ms |
| **Routing Decision Latency** | p95 < 100ms | Per request | > 200ms |
| **Provider Authorization Latency** | p95 < 2000ms | Per request | > 5000ms |
| **Reconciliation Match Rate** | > 99.9% | Daily | < 99.5% |
| **Notification Delivery Rate** | > 98% | Daily | < 95% |
| **System Availability** | > 99.95% | Monthly | < 99.9% |

---

## Summary

This payment system implements a **comprehensive, distributed architecture** that handles end-to-end payment processing with:

- **Robust validation** at multiple layers (gateway, payment, risk, routing)
- **Deterministic state machine** for payment orchestration
- **Real-time risk assessment** with configurable rules and ML model integration
- **Intelligent routing** with cost optimization and circuit breaking
- **Resilient bank integration** with retry logic and error normalization
- **Double-entry ledger** for financial integrity and audit compliance
- **Multi-channel notifications** with delivery tracking and retry logic
- **Daily reconciliation** with sophisticated matching and discrepancy handling
- **Settlement verification** and chargeback processing capabilities

The system follows **Domain-Driven Design** principles with each service owning its bounded context, communicating via synchronous HTTP APIs and asynchronous events, ensuring **loose coupling** while maintaining **strong consistency** where required for financial operations.

---

**Document Version:** 1.0  
**Last Updated:** 2026-08-06  
**Maintained By:** Payment System Architecture Team
