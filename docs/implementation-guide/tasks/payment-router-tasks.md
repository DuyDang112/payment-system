# Payment Router Service - Implementation Tasks

## Phase 1: Project Setup

### Task 1.1: Initialize Project
**Priority:** P0  
**Time:** 3 hours  

Create project structure, set up TypeScript, configure database (PostgreSQL), caching (Redis), messaging.

### Task 1.2: Define Domain Entities
**Priority:** P0  
**Time:** 4 hours  

Create entities:
- `PaymentProvider` (capabilities, cost config, health status, circuit breaker state)
- `RoutingRule` (conditions, priority, action)
- `RoutingDecision` (audit trail)

## Phase 2: Infrastructure

### Task 2.1: Database Schema
**Priority:** P0  
**Time:** 3 hours  

Tables: `payment_providers`, `routing_rules`, `routing_decisions`, `provider_health_metrics`

### Task 2.2: Repository Implementation
**Priority:** P0  
**Time:** 4 hours  

Implement repositories with Redis caching for provider configs.

## Phase 3: Core Services

### Task 3.1: Provider Selection Engine
**Priority:** P0  
**Time:** 6 hours  

Steps:
1. Implement filtering by capabilities (currency, method, amount)
2. Implement filtering by health status
3. Apply routing rules
4. Execute routing strategy:
   - COST_BASED: Calculate cost for each provider
   - PERFORMANCE: Sort by success rate
   - PRIORITY: Use defined priority
   - ROUND_ROBIN: Distribute load
5. Select provider and fallbacks
6. Create `RoutingDecision` record

### Task 3.2: Circuit Breaker Implementation
**Priority:** P0  
**Time:** 4 hours  

Steps:
1. Implement circuit breaker per provider
2. Track consecutive failures
3. Open circuit after threshold (5 failures)
4. Implement half-open state for testing
5. Close circuit after successful test
6. Emit circuit state events

### Task 3.3: Health Checking
**Priority:** P0  
**Time:** 3 hours  

Steps:
1. Implement periodic health checks
2. Call Bank Adapter health endpoints
3. Update provider health status
4. Track performance metrics (success rate, latency)
5. Emit health change events

### Task 3.4: Cost Calculator
**Priority:** P1  
**Time:** 3 hours  

Steps:
1. Calculate fixed + percentage fees
2. Apply min/max fee caps
3. Estimate cost before routing
4. Return cost estimate

## Phase 4: Integration

### Task 4.1: Event Listeners
**Priority:** P0  
**Time:** 3 hours  

Listen to:
- `PaymentAuthorizationFailed` - Update provider health
- `PaymentAuthorizationSucceeded` - Update metrics

### Task 4.2: REST API
**Priority:** P0  
**Time:** 3 hours  

Endpoints:
- `POST /api/routing/route` - Get routing decision
- `GET/POST /api/routing/providers` - Manage providers
- `GET/POST /api/routing/rules` - Manage rules

## Phase 5: Testing & Deployment

### Task 5.1: Tests
**Priority:** P1  
**Time:** 6 hours  

Unit + integration tests for routing logic, circuit breakers, health checks.

### Task 5.2: Deployment
**Priority:** P0  
**Time:** 2 hours  

Docker, health checks, migrations.

## Task Order

**Critical:** 1.1 → 1.2 → 2.1 → 2.2 → 3.1 → 3.2 → 4.2 → 5.2

## Total Time

**Minimum:** ~25 hours  
**Full:** ~35 hours
