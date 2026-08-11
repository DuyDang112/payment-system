# Payment Processing Service - Business Requirements Document (BRD)

## 1. Executive Summary

**Service Name:** Payment Processing Service  
**Version:** 1.0  
**Status:** Draft  
**Last Updated:** 2025-08-05  

### 1.1 Purpose
The Payment Processing Service orchestrates the complete payment lifecycle, coordinating between multiple bounded contexts (risk evaluation, payment routing, bank integration, ledger posting, notifications) while ensuring payment state consistency and correctness.

### 1.2 Business Objectives
- Orchestrate end-to-end payment workflows
- Ensure payment state machine integrity
- Provide idempotency guarantees
- Coordinate distributed transactions via saga pattern
- Maintain payment audit trail
- Enable payment reconciliation

## 2. Scope

### 2.1 In Scope
- Payment creation and initialization
- Payment workflow orchestration
- Payment state management (created → processing → completed/failed)
- Idempotency enforcement
- Payment retry logic
- Coordination of risk evaluation, routing, and execution
- Payment reconciliation triggers
- Payment cancellation

### 2.2 Out of Scope
- Risk evaluation rules (Risk Service)
- Bank integration logic (Bank Adapter Service)
- Routing decisions (Payment Router Service)
- Ledger accounting entries (Ledger Service)
- Notification delivery (Notification Service)

## 3. Functional Requirements

### 3.1 Payment Creation
**REQ-PP-001:** Service MUST create payment with unique payment ID  
**REQ-PP-002:** Service MUST validate payment amount (must be positive)  
**REQ-PP-003:** Service MUST validate currency (must be supported)  
**REQ-PP-004:** Service MUST enforce idempotency key uniqueness per merchant  
**REQ-PP-005:** Service MUST tokenize payment method details  
**REQ-PP-006:** Service MUST create initial payment state as CREATED  

### 3.2 Payment Orchestration
**REQ-PP-007:** Service MUST coordinate risk evaluation before routing  
**REQ-PP-008:** Service MUST proceed to routing only after risk approval  
**REQ-PP-009:** Service MUST trigger bank authorization after routing  
**REQ-PP-010:** Service MUST update payment state at each orchestration step  
**REQ-PP-011:** Service MUST implement saga pattern for distributed transactions  

### 3.3 State Machine
**REQ-PP-012:** Payment state MUST follow: CREATED → PROCESSING → RISK_EVALUATION → ROUTING → AUTHORIZING → COMPLETED/FAILED/CANCELLED  
**REQ-PP-013:** Service MUST reject invalid state transitions  
**REQ-PP-014:** Completed payments MUST be immutable  
**REQ-PP-015:** Service MUST record all state transitions in audit log  

### 3.4 Idempotency
**REQ-PP-016:** Service MUST check idempotency key before creating payment  
**REQ-PP-017:** Service MUST return existing payment if idempotency key exists  
**REQ-PP-018:** Service MUST expire idempotency keys after 24 hours  

### 3.5 Retry Logic
**REQ-PP-019:** Service MUST retry failed payments with exponential backoff  
**REQ-PP-020:** Service MUST record payment attempts  
**REQ-PP-021:** Service MUST enforce maximum retry limit (3 attempts)  
**REQ-PP-022:** Service MUST mark payment as FAILED after max retries  

### 3.6 Payment Cancellation
**REQ-PP-023:** Service MUST cancel payments in CREATED or PROCESSING state  
**REQ-PP-024:** Service MUST reject cancellation for COMPLETED payments  
**REQ-PP-025:** Service MUST notify downstream services on cancellation  

## 4. Non-Functional Requirements

### 4.1 Performance
**NFR-PP-001:** Payment creation MUST complete in < 100ms  
**NFR-PP-002:** Payment state updates MUST be strongly consistent  
**NFR-PP-003:** Service MUST support 1,000 payments per second  

### 4.2 Availability
**NFR-PP-004:** Service MUST have 99.95% uptime SLA  
**NFR-PP-005:** Service MUST partition by paymentId for horizontal scaling  

### 4.3 Consistency
**NFR-PP-006:** Payment state transitions MUST be atomic  
**NFR-PP-007:** Idempotency checks MUST be atomic with payment creation  

### 4.4 Security
**NFR-PP-008:** Service MUST never store full payment card details  
**NFR-PP-009:** Service MUST encrypt PII data at rest  
**NFR-PP-010:** Service MUST enforce tenant isolation per merchant  

### 4.5 Observability
**NFR-PP-011:** Service MUST emit domain events for all state transitions  
**NFR-PP-012:** Service MUST trace payment flow across distributed services  

## 5. Business Rules

**BR-PP-001:** Payment amount cannot be negative  
**BR-PP-002:** Currency must be in supported currency list  
**BR-PP-003:** Idempotency keys must be unique per merchant  
**BR-PP-004:** Payment must pass risk check before routing  
**BR-PP-005:** Completed payment cannot be modified  
**BR-PP-006:** Payment can only transition to valid next states  
**BR-PP-007:** Failed payments after max retries cannot be retried  
**BR-PP-008:** Cancellation is only allowed before COMPLETED state  

## 6. User Stories

### 6.1 Payment Processing
**US-PP-001:** As a merchant, I want to create payments so that I can process customer transactions  
**US-PP-002:** As a merchant, I want idempotency so that duplicate requests don't create duplicate payments  
**US-PP-003:** As a customer, I want payment status updates so that I know transaction progress  

### 6.2 Payment Operations
**US-PP-004:** As a merchant, I want to cancel pending payments so that I can stop unwanted transactions  
**US-PP-005:** As a system administrator, I want payment audit trail so that I can troubleshoot issues  

## 7. Interface Specifications

### 7.1 API Endpoints
| Endpoint | Method | Auth | Request | Response |
|----------|--------|------|---------|----------|
| `/api/payments` | POST | Bearer | `CreatePaymentRequest` | `CreatePaymentResponse` |
| `/api/payments/{id}` | GET | Bearer | N/A | `GetPaymentResponse` |
| `/api/payments/{id}/cancel` | POST | Bearer | `CancelPaymentRequest` | `CancelPaymentResponse` |

### 7.2 Events Published
| Event | When | Consumers |
|-------|------|-----------|
| `PaymentCreated` | Payment creation | Ledger, Notification |
| `PaymentRiskApproved` | Risk approval | Payment Router |
| `PaymentRiskRejected` | Risk rejection | Notification, Ledger |
| `PaymentCompleted` | Payment success | Ledger, Notification, Reconciliation |
| `PaymentFailed` | Payment failure | Notification, Ledger |

### 7.3 Events Consumed
| Event | Producer | Processing |
|-------|----------|-----------|
| `RiskEvaluationCompleted` | Risk Service | Update status, proceed to routing or rejection |
| `PaymentAuthorized` | Bank Adapter | Mark as completed |
| `PaymentAuthorizationFailed` | Bank Adapter | Mark as failed or retry |
| `LedgerEntryCreated` | Ledger Service | Confirm finalization |

## 8. Data Models

### 8.1 Payment Aggregate
```typescript
{
  paymentId: string;
  merchantId: string;
  customerId: string;
  amount: Money;
  currency: Currency;
  status: PaymentStatus; // CREATED, PROCESSING, RISK_EVALUATION, ROUTING, AUTHORIZING, COMPLETED, FAILED, CANCELLED
  idempotencyKey: string;
  createdAt: DateTime;
  updatedAt: DateTime;
  completedAt?: DateTime;
  failureReason?: string;
  metadata: Record<string, any>;
}
```

### 8.2 Payment Attempt
```typescript
{
  attemptId: string;
  paymentId: string;
  attemptNumber: number;
  startedAt: DateTime;
  completedAt?: DateTime;
  status: AttemptStatus;
  failureReason?: string;
  provider: string;
}
```

### 8.3 Money Value Object
```typescript
{
  amount: Decimal;
  currency: string;
}
```

## 9. Acceptance Criteria

### 9.1 Payment Creation
- [ ] Valid payment requests create payment with CREATED status
- [ ] Idempotency key prevents duplicate payments
- [ ] Invalid amounts are rejected
- [ ] Unsupported currencies are rejected
- [ ] Payment method details are tokenized

### 9.2 Payment Orchestration
- [ ] Payment proceeds through state machine correctly
- [ ] Risk evaluation is triggered before routing
- [ ] Failed risk evaluation rejects payment
- [ ] Successful routing triggers bank authorization
- [ ] Failed authorization triggers retry

### 9.3 State Management
- [ ] Invalid state transitions are rejected
- [ ] All state transitions are audited
- [ ] Completed payments are immutable

### 9.4 Idempotency
- [ ] Duplicate requests with same key return same payment
- [ ] Idempotency keys expire after 24 hours
- [ ] Keys are unique per merchant

## 10. Dependencies

### 10.1 Upstream Dependencies
- API Gateway
- Merchant Service (validation)

### 10.2 Downstream Dependencies
- Risk Service (sync or async)
- Payment Router Service (async)
- Bank Adapter Service (async)
- Ledger Service (async)
- Notification Service (async)

## 11. Database Schema

### 11.1 Tables
- `payments` - Payment aggregates
- `payment_attempts` - Retry history
- `payment_state_transitions` - Audit log
- `idempotency_keys` - Duplicate prevention

### 11.2 Data Patterns
- **Write:** Strong consistency (ACID)
- **Read:** Eventual consistency acceptable for most queries
- **Hot data:** Recent payments (last 7 days)
- **Cold data:** Historical payments (archive after 90 days)

## 12. Risks & Assumptions

### 12.1 Risks
- **RISK-1:** Distributed transaction complexity - Mitigation: Saga pattern with compensating transactions
- **RISK-2:** State machine inconsistency - Mitigation: Atomic state transitions with audit log
- **RISK-3:** Idempotency key collisions - Mitigation: Composite key (merchantId + key)

### 12.2 Assumptions
- Downstream services are available for orchestration
- Message broker ensures at-least-once delivery
- Risk evaluation completes within timeout

## 13. Success Metrics

- **Metric 1:** Payment creation p95 latency < 100ms
- **Metric 2:** 99.95% uptime
- **Metric 3:** Zero duplicate payments with same idempotency key
- **Metric 4:** 100% of payments have complete audit trail
- **Metric 5:** Payment success rate > 95%
