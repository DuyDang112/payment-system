# Ledger Service - Implementation Tasks

## Phase 1: Project Setup

### Task 1.1: Initialize Project
**Priority:** P0  
**Time:** 3 hours  

Set up TypeScript, dependencies, database (PostgreSQL - ACID critical), messaging.

### Task 1.2: Define Domain Entities
**Priority:** P0  
**Time:** 5 hours  

Create entities:
- `Account` (balances, overdraft)
- `JournalEntry` (header with lines)
- `JournalEntryLine` (individual debit/credit)
- `LedgerTransaction` (posted transaction)
- `Reconciliation` (for reconciliation service)

**Critical:** Implement double-entry validation (debits MUST equal credits).

## Phase 2: Infrastructure

### Task 2.1: Database Schema
**Priority:** P0  
**Time:** 4 hours  

Tables:
- `accounts` (with constraints)
- `journal_entries`
- `journal_entry_lines`
- `ledger_transactions`
- `account_balances` (daily snapshots)

**Critical:** Use database transactions for ACID compliance.

### Task 2.2: Repository Implementation
**Priority:** P0  
**Time:** 5 hours  

Implement repositories with transaction support. Balance updates MUST be atomic with journal entry posting.

## Phase 3: Core Services

### Task 3.1: Double-Entry Bookkeeping Service
**Priority:** P0  
**Time:** 6 hours  

Steps:
1. Validate journal entry (debits = credits)
2. Validate account existence
3. Post entry atomically:
   - Insert journal entry
   - Insert lines
   - Update account balances
   - Create ledger transactions
4. Rollback on any error
5. Publish `JournalEntryPosted` event

**Acceptance:** Zero balance errors, 100% of entries balance.

### Task 3.2: Balance Calculator
**Priority:** P0  
**Time:** 4 hours  

Steps:
1. Calculate current balance from ledger transactions
2. Calculate available balance (current - pending holds)
3. Support balance queries by date
4. Cache current balances in Redis

### Task 3.3: Account Management
**Priority:** P0  
**Time:** 4 hours  

Steps:
1. Create accounts (ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE)
2. Support overdraft configuration
3. Activate/deactivate accounts
4. Validate account constraints

### Task 3.4: Entry Reversal
**Priority:** P1  
**Time:** 3 hours  

Steps:
1. Create reversing entry
2. Reverse original entry's impact
3. Link reversal to original
4. Maintain audit trail

## Phase 4: Integration

### Task 4.1: Event Listeners
**Priority:** P0  
**Time:** 4 hours  

Listen to:
- `PaymentCreated` - Create pending journal entry
- `PaymentCompleted` - Finalize journal entry
- `PaymentRefunded` - Create refund entry
- `PaymentChargeback` - Create chargeback entry

### Task 4.2: REST API
**Priority:** P0  
**Time:** 4 hours  

Endpoints:
- `POST /api/ledger/journal-entries` - Create entry
- `GET /api/ledger/journal-entries/:id` - Get entry
- `GET /api/ledger/accounts/:id` - Get account
- `GET /api/ledger/accounts/:id/balance` - Get balance
- `POST /api/ledger/reconcile` - Reconcile (admin)

## Phase 5: Testing

### Task 5.1: Double-Entry Tests
**Priority:** P0  
**Time:** 6 hours  

Critical tests:
- Verify debits = credits for ALL entries
- Test balance calculations
- Test atomic posting
- Test overdraft enforcement
- Test entry reversal

### Task 5.2: Integration Tests
**Priority:** P1  
**Time:** 5 hours  

Test payment workflows, reconciliation support.

## Phase 6: Deployment

### Task 6.1: Deployment
**Priority:** P0  
**Time:** 3 hours  

Docker, database migrations, health checks.

## Task Order

**Critical:** 1.1 → 1.2 → 2.1 → 2.2 → 3.1 → 3.2 → 4.2 → 5.1 → 6.1

## Total Time

**Minimum:** ~35 hours  
**Full:** ~45 hours
