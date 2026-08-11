# Ledger Service - Business Requirements Document (BRD)

## 1. Executive Summary

**Service Name:** Ledger Service  
**Version:** 1.0  
**Status:** Draft  
**Last Updated:** 2025-08-05  

### 1.1 Purpose
The Ledger Service maintains accurate financial records using double-entry bookkeeping, manages account balances, provides reconciliation support, and ensures audit trail compliance for the payment system.

### 1.2 Business Objectives
- Maintain accurate financial records
- Ensure double-entry bookkeeping integrity
- Support account balance management
- Enable reconciliation processes
- Provide audit trail compliance
- Support financial reporting

## 2. Scope

### 2.1 In Scope
- Double-entry transaction recording
- Account balance management
- Journal entry creation and posting
- Financial transaction posting
- Account reconciliation
- Audit trail maintenance
- Financial reporting data

### 2.2 Out of Scope
- Payment processing logic
- Bank API integration
- Risk evaluation
- Customer data management
- Reporting dashboards (reporting service)

## 3. Functional Requirements

### 3.1 Double-Entry Bookkeeping
**REQ-LG-001:** Service MUST enforce debits = credits for every transaction  
**REQ-LG-002:** Service MUST create journal entries with at least 2 lines  
**REQ-LG-003:** Service MUST post journal entries atomically  
**REQ-LG-004:** Service MUST update account balances on posting  
**REQ-LG-005:** Service MUST maintain transaction audit trail  

### 3.2 Account Management
**REQ-LG-006:** Service MUST support account types (ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE)  
**REQ-LG-007:** Service MUST track current and available balances  
**REQ-LG-008:** Service MUST prevent negative balances unless overdraft enabled  
**REQ-LG-009:** Service MUST support account holds (pending transactions)  

### 3.3 Journal Entry Processing
**REQ-LG-010:** Service MUST create pending journal entries for payments  
**REQ-LG-011:** Service MUST finalize journal entries on payment completion  
**REQ-LG-012:** Service MUST support entry reversal  
**REQ-LG-013:** Service MUST assign sequential entry numbers  

### 3.4 Reconciliation Support
**REQ-LG-014:** Service MUST provide account balances for reconciliation  
**REQ-LG-015:** Service MUST support reconciliation queries by period  
**REQ-LG-016:** Service MUST match transactions with external providers  

### 3.5 Audit Trail
**REQ-LG-017:** Service MUST log all balance changes  
**REQ-LG-018:** Service MUST record who posted each entry  
**REQ-LG-019:** Service MUST maintain immutable posted entries  

### 3.6 Financial Reporting
**REQ-LG-020:** Service MUST provide trial balance data  
**REQ-LG-021:** Service MUST support transaction history queries  
**REQ-LG-022:** Service MUST support balance history queries  

## 4. Non-Functional Requirements

### 4.1 Consistency
**NFR-LG-001:** Journal entry posting MUST be atomic (ACID)  
**NFR-LG-002:** Balance updates MUST be consistent with journal entries  

### 4.2 Performance
**NFR-LG-003:** Journal entry creation MUST complete in < 100ms  
**NFR-LG-004:** Balance queries MUST complete in < 50ms  

### 4.3 Security
**NFR-LG-005:** Financial data MUST be encrypted at rest  
**NFR-LG-006:** Access MUST be role-based (accountant, admin, auditor)  
**NFR-LG-007:** All operations MUST be audited  

### 4.4 Compliance
**NFR-LG-008:** Posted entries MUST be immutable  
**NFR-LG-009:** Transaction history MUST be retained for 7 years  

## 5. Business Rules

**BR-LG-001:** Every transaction must balance (debits = credits)  
**BR-LG-002:** Account balances must never go negative unless overdraft enabled  
**BR-LG-003:** Posted transactions must be immutable  
**BR-LG-004:** Every journal entry must have audit trail  
**BR-LG-005:** Reconciliation must match external provider records  

## 6. Interface Specifications

### 6.1 API Endpoints
| Endpoint | Method | Auth | Request | Response |
|----------|--------|------|---------|----------|
| `/api/ledger/journal-entries` | POST | Internal | `CreateJournalEntryRequest` | `CreateJournalEntryResponse` |
| `/api/ledger/journal-entries/{id}` | GET | Internal | N/A | `GetJournalEntryResponse` |
| `/api/ledger/accounts/{id}` | GET | Internal | N/A | `GetAccountResponse` |
| `/api/ledger/accounts/{id}/balance` | GET | Internal | N/A | `GetBalanceResponse` |
| `/api/ledger/reconcile` | POST | Admin | `ReconcileRequest` | `ReconcileResponse` |
| `/api/ledger/transactions` | GET | Admin | N/A | `ListTransactionsResponse` |

### 6.2 Events Published
| Event | When | Consumers |
|-------|------|-----------|
| `JournalEntryPosted` | Entry posting | Payment Service, Reconciliation |
| `AccountBalanceChanged` | Balance update | Payment Service, Notification |
| `ReconciliationCompleted` | Reconciliation completion | Reporting, Audit |
| `ReconciliationFailed` | Reconciliation failure | Alerting, Operations |

### 6.3 Events Consumed
| Event | Producer | Processing |
|-------|----------|-----------|
| `PaymentCreated` | Payment Service | Create pending journal entry |
| `PaymentCompleted` | Payment Service | Post journal entry, update balances |
| `PaymentRefunded` | Payment Service | Create refund journal entry |
| `PaymentChargeback` | Payment Service | Create chargeback journal entry |

## 7. Data Models

### 7.1 Account
```typescript
{
  accountId: string;
  accountNumber: string;
  accountType: AccountType; // ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE
  accountSubtype: AccountSubtype;
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
```

### 7.2 Journal Entry
```typescript
{
  entryId: string;
  entryDate: DateTime;
  entryNumber: string;
  referenceType: ReferenceType; // PAYMENT, REFUND, CHARGEBACK, ADJUSTMENT
  referenceId: string;
  description: string;
  status: EntryStatus; // PENDING, POSTED, REVERSED
  totalDebit: Money;
  totalCredit: Money;
  lines: JournalEntryLine[];
  postedAt?: DateTime;
  createdBy: string;
  createdAt: DateTime;
  updatedAt: DateTime;
}
```

### 7.3 Journal Entry Line
```typescript
{
  lineId: string;
  entryId: string;
  accountId: string;
  debitAmount: Money;
  creditAmount: Money;
  description: string;
  lineSequence: number;
}
```

## 8. Acceptance Criteria

- [ ] All journal entries balance (debits = credits)
- [ ] Account balances are accurate
- [ ] Posted entries are immutable
- [ ] Audit trail is complete
- [ ] Negative balances prevented (unless overdraft)
- [ ] Reconciliation is supported

## 9. Success Metrics

- **Metric 1:** Journal entry posting p95 latency < 100ms
- **Metric 2:** Balance query p95 latency < 50ms
- **Metric 3:** 100% of entries balance
- **Metric 4:** Zero data loss in posting
- **Metric 5:** Reconciliation match rate > 99.9%
