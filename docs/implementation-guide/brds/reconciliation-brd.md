# Reconciliation Service - Business Requirements Document (BRD)

## 1. Executive Summary

**Service Name:** Reconciliation Service  
**Version:** 1.0  
**Status:** Draft  
**Last Updated:** 2025-08-05  

### 1.1 Purpose
The Reconciliation Service reconciles internal records with external payment providers, detects and resolves discrepancies, ensures financial data integrity, supports settlement and chargeback processes, and generates reconciliation reports.

### 1.2 Business Objectives
- Reconcile internal records with external providers daily
- Detect and resolve discrepancies
- Ensure financial data integrity
- Support settlement tracking and verification
- Manage chargeback processing
- Generate reconciliation reports
- Maintain audit trail compliance

## 2. Scope

### 2.1 In Scope
- Daily reconciliation with providers
- Discrepancy detection and resolution
- Settlement tracking and verification
- Chargeback processing and response
- Reconciliation reporting
- Matching algorithm execution
- Audit trail maintenance

### 2.2 Out of Scope
- Payment processing logic
- Bank API integration (uses Bank Adapter)
- Ledger accounting entries
- Notification delivery

## 3. Functional Requirements

### 3.1 Reconciliation Execution
**REQ-RC-001:** Service MUST run daily reconciliation for each provider  
**REQ-RC-002:** Service MUST match internal transactions with provider records  
**REQ-RC-003:** Service MUST identify unmatched transactions  
**REQ-RC-004:** Service MUST calculate reconciliation summary (matched, unmatched, discrepancies)  
**REQ-RC-005:** Service MUST support ad-hoc reconciliation runs  
**REQ-RC-006:** Service MUST complete reconciliation within SLA (24 hours)  

### 3.2 Discrepancy Management
**REQ-RC-007:** Service MUST detect discrepancy types (missing internal, missing provider, amount mismatch, status mismatch, timing mismatch)  
**REQ-RC-008:** Service MUST assign severity to discrepancies  
**REQ-RC-009:** Service MUST track discrepancy resolution  
**REQ-RC-010:** Service MUST notify operations team of critical discrepancies  
**REQ-RC-011:** Service MUST support discrepancy investigation workflow  

### 3.3 Settlement Processing
**REQ-RC-012:** Service MUST receive and process provider settlement batches  
**REQ-RC-013:** Service MUST verify settlement amounts against internal records  
**REQ-RC-014:** Service MUST identify settlement discrepancies  
**REQ-RC-015:** Service MUST track settlement status (pending, received, verified, discrepancy)  

### 3.4 Chargeback Management
**REQ-RC-016:** Service MUST receive chargeback notifications from providers  
**REQ-RC-017:** Service MUST track chargeback response deadlines  
**REQ-RC-018:** Service MUST support chargeback evidence submission  
**REQ-RC-019:** Service MUST track chargeback outcomes (won, lost, expired)  
**REQ-RC-020:** Service MUST calculate chargeback liability  

### 3.5 Matching Algorithms
**REQ-RC-021:** Service MUST support multiple matching strategies (exact, fuzzy, amount-based)  
**REQ-RC-022:** Service MUST calculate match confidence scores  
**REQ-RC-023:** Service MUST support manual matching for edge cases  

### 3.6 Reporting
**REQ-RC-024:** Service MUST generate daily reconciliation reports  
**REQ-RC-025:** Service MUST provide discrepancy summary reports  
**REQ-RC-026:** Service MUST track reconciliation metrics (match rate, discrepancy rate)  

## 4. Non-Functional Requirements

### 4.1 Performance
**NFR-RC-001:** Daily reconciliation MUST complete within 4 hours  
**NFR-RC-002:** Matching algorithm MUST handle 100K transactions per run  

### 4.2 Accuracy
**NFR-RC-003:** Matching accuracy MUST be > 99.5%  
**NFR-RC-004:** Zero false positives in discrepancy detection  

### 4.3 Security
**NFR-RC-005:** Financial data MUST be encrypted at rest  
**NFR-RC-006:** Access MUST be role-based (reconciliation analyst, admin)  
**NFR-RC-007:** All operations MUST be audited  

## 5. Business Rules

**BR-RC-001:** All transactions must be reconciled within SLA (24 hours)  
**BR-RC-002:** Discrepancies must be investigated and resolved  
**BR-RC-003:** Reconciliation must balance (matches + discrepancies = total)  
**BR-RC-004:** Chargebacks must be processed within provider deadlines  
**BR-RC-005:** Settlements must be tracked and verified  

## 6. Interface Specifications

### 6.1 API Endpoints
| Endpoint | Method | Auth | Request | Response |
|----------|--------|------|---------|----------|
| `/api/reconciliation/run` | POST | Admin | `RunReconciliationRequest` | `RunReconciliationResponse` |
| `/api/reconciliation/runs/{id}` | GET | Admin | N/A | `GetRunResponse` |
| `/api/reconciliation/discrepancies` | GET | Admin | N/A | `ListDiscrepanciesResponse` |
| `/api/reconciliation/discrepancies/{id}/resolve` | POST | Admin | `ResolveDiscrepancyRequest` | `ResolveDiscrepancyResponse` |
| `/api/reconciliation/settlements` | GET | Admin | N/A | `ListSettlementsResponse` |
| `/api/reconciliation/settlements/{id}/verify` | POST | Admin | `VerifySettlementRequest` | `VerifySettlementResponse` |
| `/api/reconciliation/chargebacks` | GET | Admin | N/A | `ListChargebacksResponse` |
| `/api/reconciliation/chargebacks/{id}/respond` | POST | Admin | `RespondChargebackRequest` | `RespondChargebackResponse` |

### 6.2 Events Published
| Event | When | Consumers |
|-------|------|-----------|
| `ReconciliationCompleted` | Successful run | Reporting, Ledger |
| `DiscrepancyDetected` | Discrepancy found | Alerting, Operations |
| `SettlementReceived` | Settlement receipt | Ledger, Finance |
| `ChargebackReceived` | Chargeback initiation | Payment, Ledger, Alerting |

### 6.3 Events Consumed
| Event | Producer | Processing |
|-------|----------|-----------|
| `PaymentCompleted` | Payment Service | Include in next reconciliation |
| `ProviderStatementReceived` | Bank Adapter | Trigger reconciliation |

## 7. Data Models

### 7.1 Reconciliation Run
```typescript
{
  runId: string;
  runType: RunType; // DAILY, AD_HOC, PROVIDER_SPECIFIC
  providerId: string;
  periodStart: DateTime;
  periodEnd: DateTime;
  status: ReconciliationStatus; // RUNNING, COMPLETED, FAILED, PARTIALLY_COMPLETED
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

### 7.2 Discrepancy
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
  status: DiscrepancyStatus; // OPEN, INVESTIGATING, RESOLVED, IGNORED
  resolution?: string;
  resolvedBy?: string;
  resolvedAt?: DateTime;
  createdAt: DateTime;
  updatedAt: DateTime;
}
```

### 7.3 Settlement
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
  status: SettlementStatus; // PENDING, RECEIVED, VERIFIED, DISCREPANCY
  verifiedAt?: DateTime;
  discrepancies: string[];
  receivedAt: DateTime;
}
```

### 7.4 Chargeback
```typescript
{
  chargebackId: string;
  providerId: string;
  paymentId: string;
  chargebackAmount: Money;
  chargebackReason: string;
  chargebackDate: Date;
  status: ChargebackStatus; // PENDING, RESPONDED, WON, LOST, EXPIRED
  responseDeadline: DateTime;
  responseEvidence?: string;
  respondedAt?: DateTime;
  outcome?: ChargebackOutcome; // FULL_REVERSAL, PARTIAL_REVERSAL, WON
  createdAt: DateTime;
  updatedAt: DateTime;
}
```

## 8. Acceptance Criteria

- [ ] Daily reconciliation completes within SLA
- [ ] All discrepancies are detected
- [ ] Settlements are verified
- [ ] Chargebacks are processed within deadlines
- [ ] Reports are generated accurately
- [ ] Audit trail is complete

## 9. Success Metrics

- **Metric 1:** Daily reconciliation completion rate = 100%
- **Metric 2:** Matching accuracy > 99.5%
- **Metric 3:** Discrepancy resolution rate > 95% within 7 days
- **Metric 4:** Chargeback response deadline compliance = 100%
- **Metric 5:** Settlement verification accuracy > 99.9%
