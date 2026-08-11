# Risk Assessment Service - Implementation Tasks

## Overview
This document breaks down the Risk Assessment Service implementation into actionable tasks.

## Phase 1: Project Setup & Domain Layer

### Task 1.1: Initialize Project & Setup
**Priority:** P0  
**Estimated Time:** 3 hours  

**Steps:**
1. Create DDD-layered project structure
2. Set up TypeScript, dependencies
3. Configure database (PostgreSQL)
4. Configure caching (Redis)
5. Set up messaging (Kafka/RabbitMQ)
6. Configure environment variables

**Acceptance Criteria:**
- [ ] Project builds successfully
- [ ] Database connects
- [ ] Redis connects
- [ ] Message broker connects

### Task 1.2: Define Domain Entities
**Priority:** P0  
**Estimated Time:** 5 hours  

**Steps:**
1. Create `RiskEvaluation` aggregate:
   - Properties: evaluationId, paymentId, riskScore, decision, triggeredRules
   - Methods: calculateScore(), makeDecision()
2. Create `RiskRule` entity:
   - Properties: ruleId, ruleType, priority, conditions, action
   - Methods: evaluate(), isActive()
3. Create `MerchantRiskProfile` entity:
   - Properties: merchantId, thresholds, enabledRules, limits
   - Methods: getThreshold(), isEnabled()
4. Create `RiskEvaluationHistory` entity
5. Define value objects: RiskScore, RuleTrigger, VelocityCheckResult

**Acceptance Criteria:**
- [ ] Entities enforce business rules
- [ ] Risk scores are 0-100
- [ ] Decisions are APPROVE/REJECT/REVIEW

### Task 1.3: Define Rule Engine Interface
**Priority:** P0  
**Estimated Time:** 3 hours  

**Steps:**
1. Create `IRuleEngine` interface
2. Define rule evaluation methods
3. Create rule condition DSL (JSON-based)
4. Define rule types:
   - VELOCITY_RULE
   - BLACKLIST_RULE
   - AMOUNT_RULE
   - GEO_RULE
   - ML_MODEL_RULE

**Acceptance Criteria:**
- [ ] Rule engine interface is defined
- [ ] Rule types support all use cases

## Phase 2: Infrastructure Layer

### Task 2.1: Implement Database Schema
**Priority:** P0  
**Estimated Time:** 4 hours  

**Steps:**
1. Create migrations for tables:
   - `risk_rules`
   - `risk_evaluations`
   - `merchant_risk_profiles`
   - `risk_evaluation_history`
   - `velocity_counters`
2. Add indexes for performance
3. Add foreign keys

**Acceptance Criteria:**
- [ ] Migrations run successfully
- [ ] Schema supports all queries

### Task 2.2: Implement Repository Pattern
**Priority:** P0  
**Estimated Time:** 4 hours  

**Steps:**
1. Implement `RiskRuleRepository`
2. Implement `RiskEvaluationRepository`
3. Implement `MerchantRiskProfileRepository`
4. Add Redis caching for profiles
5. Add transaction support

**Acceptance Criteria:**
- [ ] Repositories implement interfaces
- [ ] Caching reduces latency
- [ ] Transactions ensure consistency

### Task 2.3: Implement Velocity Checker
**Priority:** P0  
**Estimated Time:** 4 hours  

**Steps:**
1. Create velocity counter storage (Redis)
2. Implement velocity check logic:
   - Track transactions per customer
   - Support multiple windows (1min, 5min, 1hour, 1day)
   - Count and aggregate
3. Add threshold evaluation
4. Handle counter expiration

**Acceptance Criteria:**
- [ ] Velocity is tracked accurately
- [ ] Multiple windows work
- [ ] Counters expire correctly

### Task 2.4: Implement Blacklist/Whitelist
**Priority:** P1  
**Estimated Time:** 3 hours  

**Steps:**
1. Create blacklist storage (Redis or DB)
2. Create whitelist storage
3. Implement check methods
4. Add CRUD operations for lists
5. Add caching for fast lookups

**Acceptance Criteria:**
- [ ] Blacklist prevents transactions
- [ ] Whitelist bypasses checks
- [ ] Lookups are fast (<10ms)

## Phase 3: Application Layer

### Task 3.1: Implement Rule Engine
**Priority:** P0  
**Estimated Time:** 8 hours  

**Steps:**
1. Implement rule evaluation engine:
   - Parse rule conditions
   - Evaluate rules by priority
   - Calculate score impacts
   - Track triggered rules
2. Implement velocity rules
3. Implement blacklist rules
4. Implement amount rules
5. Implement geo rules (optional: ML model integration)

**Acceptance Criteria:**
- [ ] Rules evaluate correctly
- [ ] Priority is respected
- [ ] Score impacts are accurate
- [ ] Triggered rules are logged

### Task 3.2: Implement Risk Evaluation Command
**Priority:** P0  
**Estimated Time:** 5 hours  

**Steps:**
1. Create `EvaluateRiskCommand`
2. Create `EvaluateRiskHandler`:
   - Load merchant profile
   - Execute rule engine
   - Calculate final score
   - Make decision
   - Save evaluation
   - Publish `RiskEvaluationCompleted` event
3. Add timeout handling (500ms max)
4. Implement fallback on timeout

**Acceptance Criteria:**
- [ ] Evaluation completes < 500ms
- [ ] Decision is deterministic
- [ ] Timeout triggers fallback
- [ ] Event is published

### Task 3.3: Implement Rule Management
**Priority:** P1  
**Estimated Time:** 4 hours  

**Steps:**
1. Create `CreateRuleCommand`
2. Create `UpdateRuleCommand`
3. Create `DeactivateRuleCommand`
4. Implement handlers with validation
5. Add rule versioning

**Acceptance Criteria:**
- [ ] Rules can be created/updated
- [ ] Rule versions are tracked
- [ ] Inactive rules are not evaluated

### Task 3.4: Implement Merchant Profile Management
**Priority:** P1  
**Estimated Time:** 4 hours  

**Steps:**
1. Create `UpdateMerchantProfileCommand`
2. Implement handler:
   - Update thresholds
   - Update enabled rules
   - Update whitelists/blacklists
3. Add validation
4. Cache profile updates

**Acceptance Criteria:**
- [ ] Profiles can be updated
- [ ] Updates are cached
- [ ] Invalid profiles are rejected

### Task 3.5: Implement Event Listeners
**Priority:** P1  
**Estimated Time:** 3 hours  

**Steps:**
1. Listen for `PaymentCreated` events
2. Trigger risk evaluation
3. Listen for `PaymentCompleted` events
4. Update evaluation history with outcome
5. Listen for `PaymentChargeback` events
6. Mark as fraud in history

**Acceptance Criteria:**
- [ ] PaymentCreated triggers evaluation
- [ ] Outcomes are recorded
- [ ] Chargebacks update history

## Phase 4: API Layer

### Task 4.1: Implement REST API
**Priority:** P0  
**Estimated Time:** 4 hours  

**Steps:**
1. Create `POST /api/risk/evaluate` (internal)
2. Create `GET /api/risk/rules` (admin)
3. Create `POST /api/risk/rules` (admin)
4. Create `GET /api/risk/merchants/:id/profile` (internal)
5. Create `PUT /api/risk/merchants/:id/profile` (admin)
6. Add authentication (internal vs admin)

**Acceptance Criteria:**
- [ ] Endpoints are accessible
- [ ] Internal/auth is enforced
- [ ] Request validation works

## Phase 5: Testing

### Task 5.1: Unit Tests
**Priority:** P1  
**Estimated Time:** 8 hours  

**Steps:**
1. Test rule engine:
   - Each rule type
   - Priority ordering
   - Score calculation
2. Test velocity checker
3. Test blacklist/whitelist
4. Test evaluation command
5. Test domain entities
6. Achieve >80% coverage

**Acceptance Criteria:**
- [ ] All tests pass
- [ ] Coverage > 80%
- [ ] Edge cases are tested

### Task 5.2: Integration Tests
**Priority:** P1  
**Estimated Time:** 5 hours  

**Steps:**
1. Test full evaluation flow
2. Test rule management
3. Test profile management
4. Test event publishing
5. Test timeout scenarios
6. Test with mock payment data

**Acceptance Criteria:**
- [ ] End-to-end flow works
- [ ] Events are published
- [ ] Timeouts are handled

## Phase 6: Deployment

### Task 6.1: Docker & Deployment
**Priority:** P0  
**Estimated Time:** 3 hours  

**Steps:**
1. Create Dockerfile
2. Add startup migrations
3. Configure health checks
4. Create docker-compose

**Acceptance Criteria:**
- [ ] Docker image builds
- [ ] Migrations run
- [ ] Health checks pass

## Task Order Summary

**Critical Path:**
1.1 → 1.2 → 2.1 → 2.2 → 2.3 → 3.1 → 3.2 → 4.1 → 6.1

**Parallel Work:**
- Tasks 1.3, 2.4 (with 2.2)
- Tasks 3.3, 3.4, 3.5 (with 3.2)
- Tasks 5.1, 5.2 (with 4.1)

## Total Estimated Time

**Minimum:** ~40 hours  
**Full:** ~60 hours
