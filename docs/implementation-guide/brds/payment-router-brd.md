# Payment Router Service - Business Requirements Document (BRD)

## 1. Executive Summary

**Service Name:** Payment Router Service  
**Version:** 1.0  
**Status:** Draft  
**Last Updated:** 2025-08-05  

### 1.1 Purpose
The Payment Router Service intelligently routes payments to appropriate payment providers/banks based on cost, success rate, availability, and business rules, providing circuit breaking, failover, and cost optimization.

### 1.2 Business Objectives
- Optimize payment routing for business metrics
- Minimize payment processing costs
- Maximize payment success rates
- Ensure resilience through circuit breaking
- Enable intelligent provider selection
- Support merchant routing preferences

## 2. Scope

### 2.1 In Scope
- Provider selection and ranking
- Cost-based routing optimization
- Circuit breaking and health checking
- Provider performance tracking
- Routing rule configuration
- Fallback and retry logic
- Provider onboarding and management

### 2.2 Out of Scope
- Payment processing logic
- Bank API integration
- Risk evaluation
- Ledger accounting

## 3. Functional Requirements

### 3.1 Provider Selection
**REQ-PR-001:** Service MUST select provider based on routing rules  
**REQ-PR-002:** Service MUST respect provider constraints (currency, payment method, amount)  
**REQ-PR-003:** Service MUST only route to healthy providers  
**REQ-PR-004:** Service MUST support multiple routing strategies (cost-based, performance, priority)  
**REQ-PR-005:** Service MUST provide fallback providers  

### 3.2 Cost Optimization
**REQ-PR-006:** Service MUST calculate processing cost per provider  
**REQ-PR-007:** Service MUST support fixed and percentage fee structures  
**REQ-PR-008:** Service MUST route to lowest-cost provider (when strategy allows)  
**REQ-PR-009:** Service MUST estimate cost before routing  

### 3.3 Circuit Breaking
**REQ-PR-010:** Service MUST implement circuit breaker per provider  
**REQ-PR-011:** Service MUST open circuit after 5 consecutive failures  
**REQ-PR-012:** Service MUST close circuit after provider health check passes  
**REQ-PR-013:** Service MUST support half-open state for testing  

### 3.4 Health Checking
**REQ-PR-014:** Service MUST perform periodic health checks on providers  
**REQ-PR-015:** Service MUST update provider health status  
**REQ-PR-016:** Service MUST emit health change events  

### 3.5 Performance Tracking
**REQ-PR-017:** Service MUST track provider success rate  
**REQ-PR-018:** Service MUST track provider latency (p50, p95, p99)  
**REQ-PR-019:** Service MUST track daily volume per provider  
**REQ-PR-020:** Service MUST use performance data for routing decisions  

### 3.6 Routing Rules
**REQ-PR-021:** Service MUST support merchant-specific routing rules  
**REQ-PR-022:** Service MUST support rule conditions (currency, method, amount, country)  
**REQ-PR-023:** Service MUST prioritize rules by priority  

## 4. Non-Functional Requirements

### 4.1 Performance
**NFR-PR-001:** Routing decision MUST complete in < 100ms  
**NFR-PR-002:** Service MUST support 5,000 routing decisions per second  

### 4.2 Availability
**NFR-PR-003:** Service MUST have 99.95% uptime SLA  
**NFR-PR-004:** Service MUST cache provider configurations  

### 4.3 Security
**NFR-PR-005:** Provider credentials must be securely stored  
**NFR-PR-006:** Routing changes must be audited  

## 5. Business Rules

**BR-PR-001:** Must route to healthy providers only  
**BR-PR-002:** Must respect merchant provider preferences  
**BR-PR-003:** Must respect currency and payment method constraints  
**BR-PR-004:** Must have at least one fallback provider  
**BR-PR-005:** Circuit breaker must open after 5 consecutive failures  
**BR-PR-006:** Routing must be deterministic (same input → same provider unless configured)  

## 6. Interface Specifications

### 6.1 API Endpoints
| Endpoint | Method | Auth | Request | Response |
|----------|--------|------|---------|----------|
| `/api/routing/route` | POST | Internal | `RoutePaymentRequest` | `RoutePaymentResponse` |
| `/api/routing/providers` | GET | Admin | N/A | `ListProvidersResponse` |
| `/api/routing/providers` | POST | Admin | `AddProviderRequest` | `AddProviderResponse` |
| `/api/routing/providers/{id}/health` | GET | Admin | N/A | `ProviderHealthResponse` |
| `/api/routing/rules` | GET | Admin | N/A | `ListRulesResponse` |
| `/api/routing/rules` | POST | Admin | `CreateRuleRequest` | `CreateRuleResponse` |

### 6.2 Events Published
| Event | When | Consumers |
|-------|------|-----------|
| `PaymentRouteSelected` | Routing decision | Payment Processing, Bank Adapter |
| `ProviderHealthChanged` | Health status change | Payment Processing, Alerting |
| `ProviderCircuitOpened` | Circuit breaker open | Alerting, Operations |
| `ProviderCircuitClosed` | Circuit breaker close | Alerting |

### 6.3 Events Consumed
| Event | Producer | Processing |
|-------|----------|-----------|
| `PaymentAuthorizationFailed` | Bank Adapter | Update provider health |
| `PaymentAuthorizationSucceeded` | Bank Adapter | Update provider metrics |

## 7. Data Models

### 7.1 Payment Provider
```typescript
{
  providerId: string;
  providerName: string;
  providerType: ProviderType; // BANK, PAYMENT_GATEWAY, WALLET
  supportedCurrencies: string[];
  supportedMethods: PaymentMethod[];
  priority: number;
  isEnabled: boolean;
  costConfig: {
    fixedFee: Money;
    percentageFee: number;
    minFee?: Money;
    maxFee?: Money;
  };
  performanceMetrics: {
    successRate: number;
    p50Latency: number;
    p95Latency: number;
    p99Latency: number;
    dailyVolume: number;
    failureRate: number;
  };
  healthStatus: HealthStatus; // HEALTHY, DEGRADED, UNHEALTHY
  circuitBreakerState: CircuitState; // CLOSED, OPEN, HALF_OPEN
}
```

### 7.2 Routing Decision
```typescript
{
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

## 8. Acceptance Criteria

- [ ] Healthy providers only selected
- [ ] Circuit breakers prevent unhealthy provider routing
- [ ] Cost calculations are accurate
- [ ] Provider constraints are respected
- [ ] Fallback providers are available
- [ ] Performance metrics are tracked

## 9. Success Metrics

- **Metric 1:** Routing decision p95 latency < 100ms
- **Metric 2:** Provider success rate tracking > 95% accuracy
- **Metric 3:** Cost optimization saves > 10% vs random routing
- **Metric 4:** Circuit breaker prevents > 90% of failed provider calls
