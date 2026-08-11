# Risk Assessment Service - Business Requirements Document (BRD)

## 1. Executive Summary

**Service Name:** Risk Assessment Service  
**Version:** 1.0  
**Status:** Draft  
**Last Updated:** 2025-08-05  

### 1.1 Purpose
The Risk Assessment Service evaluates payment transactions for fraud and risk, making accept/reject decisions based on configurable business rules, protecting merchants from fraudulent transactions while maintaining regulatory compliance.

### 1.2 Business Objectives
- Prevent fraudulent transactions
- Reduce chargebacks
- Ensure regulatory compliance (AML, KYC)
- Enable merchant-specific risk profiles
- Provide real-time risk evaluation
- Maintain risk evaluation consistency

## 2. Scope

### 2.1 In Scope
- Real-time risk evaluation
- Fraud rule engine execution
- Risk score calculation
- Merchant risk profile management
- Risk rule configuration
- Risk event logging and audit
- Velocity checking
- Blacklist/whitelist management
- Geographic location checks
- ML model integration (optional)

### 2.2 Out of Scope
- Payment processing logic
- Bank integration
- Ledger accounting
- Customer data management (references only)
- Fraud investigation (discrepancy handling)

## 3. Functional Requirements

### 3.1 Risk Evaluation
**REQ-RS-001:** Service MUST evaluate payment risk within 500ms timeout  
**REQ-RS-002:** Service MUST produce deterministic results (same input → same output)  
**REQ-RS-003:** Service MUST calculate risk score (0-100)  
**REQ-RS-004:** Service MUST make accept/reject/review decision  
**REQ-RS-005:** Service MUST log triggered rules for audit  
**REQ-RS-006:** Service MUST be idempotent (same request → same decision)  

### 3.2 Rule Engine
**REQ-RS-007:** Service MUST support rule versioning  
**REQ-RS-008:** Service MUST evaluate rules by priority order  
**REQ-RS-009:** Service MUST support rule conditions (velocity, blacklist, amount, geo, ML)  
**REQ-RS-010:** Service MUST track rule performance metrics  

### 3.3 Merchant Profiles
**REQ-RS-011:** Service MUST maintain merchant-specific risk profiles  
**REQ-RS-012:** Service MUST support merchant whitelisted/blacklisted countries  
**REQ-RS-013:** Service MUST support merchant-specific velocity limits  
**REQ-RS-014:** Service MUST support merchant risk thresholds (auto-reject, manual review)  

### 3.4 Velocity Checking
**REQ-RS-015:** Service MUST track transaction velocity per customer  
**REQ-RS-016:** Service MUST support configurable velocity windows (1min, 5min, 1hour, 1day)  
**REQ-RS-017:** Service MUST flag transactions exceeding velocity limits  

### 3.5 Blacklist/Whitelist
**REQ-RS-018:** Service MUST maintain customer blacklist  
**REQ-RS-019:** Service MUST maintain customer whitelist  
**REQ-RS-020:** Service MUST check blacklist/whitelist before rule evaluation  

### 3.6 Geographic Checks
**REQ-RS-021:** Service MUST validate customer country against merchant whitelist  
**REQ-RS-022:** Service MUST flag high-risk geographic regions  
**REQ-RS-023:** Service MUST support IP-based geolocation  

### 3.7 Risk Analytics
**REQ-RS-024:** Service MUST track evaluation history  
**REQ-RS-025:** Service MUST record actual outcomes (fraud, legit, chargeback, refund)  
**REQ-RS-026:** Service MUST provide feedback for model improvement  

## 4. Non-Functional Requirements

### 4.1 Performance
**NFR-RS-001:** Risk evaluation MUST complete in < 500ms  
**NFR-RS-002:** Service MUST support 1,000 evaluations per second  
**NFR-RS-003:** Merchant profiles MUST be cached in Redis  

### 4.2 Availability
**NFR-RS-004:** Service MUST have 99.95% uptime SLA  
**NFR-RS-005:** Service MUST fail open on timeout (configurable)  

### 4.3 Security
**NFR-RS-006:** Risk evaluation data must be encrypted at rest  
**NFR-RS-007:** Rule changes must be audited  
**NFR-RS-008:** Merchant profiles must be isolated (tenant separation)  

### 4.4 Observability
**NFR-RS-009:** Service MUST emit evaluation metrics  
**NFR-RS-010:** Service MUST log all triggered rules  

## 5. Business Rules

**BR-RS-001:** High-risk transactions (score > 80) must be rejected  
**BR-RS-002:** Medium-risk transactions (score 50-80) must require manual review  
**BR-RS-003:** Low-risk transactions (score < 50) must be auto-approved  
**BR-RS-004:** Blacklisted customers must always be rejected  
**BR-RS-005:** Whitelisted customers must bypass velocity checks  
**BR-RS-006:** Risk rules must be versioned and immutable  
**BR-RS-007:** Evaluation timeout must default to APPROVE with warning  

## 6. User Stories

### 6.1 Risk Management
**US-RS-001:** As a fraud manager, I want to configure risk rules so that I can prevent fraud  
**US-RS-002:** As a merchant, I want custom risk profiles so that I can set my risk tolerance  
**US-RS-003:** As a fraud analyst, I want to see triggered rules so that I can investigate  

### 6.2 Risk Evaluation
**US-RS-004:** As a payment system, I want real-time risk evaluation so that fraud is blocked instantly  
**US-RS-005:** As a compliance officer, I want risk audit trail so that I can meet regulations  

## 7. Interface Specifications

### 7.1 API Endpoints
| Endpoint | Method | Auth | Request | Response |
|----------|--------|------|---------|----------|
| `/api/risk/evaluate` | POST | Internal | `EvaluateRiskRequest` | `EvaluateRiskResponse` |
| `/api/risk/rules` | GET | Admin | N/A | `ListRulesResponse` |
| `/api/risk/rules` | POST | Admin | `CreateRuleRequest` | `CreateRuleResponse` |
| `/api/risk/merchants/{id}/profile` | GET | Internal | N/A | `GetMerchantProfileResponse` |
| `/api/risk/merchants/{id}/profile` | PUT | Admin | `UpdateProfileRequest` | `UpdateProfileResponse` |
| `/api/risk/evaluations/{id}` | GET | Admin | N/A | `GetEvaluationResponse` |

### 7.2 Events Published
| Event | When | Consumers |
|-------|------|-----------|
| `RiskEvaluationCompleted` | Evaluation completion | Payment Processing, Analytics |
| `RiskEvaluationFailed` | Evaluation error | Payment Processing, Alerting |
| `SuspiciousActivityDetected` | Pattern detection | Fraud Investigation, Alerting |

### 7.3 Events Consumed
| Event | Producer | Processing |
|-------|----------|-----------|
| `PaymentCreated` | Payment Service | Trigger risk evaluation |
| `PaymentCompleted` | Payment Service | Update risk models (feedback) |
| `PaymentChargeback` | Ledger Service | Mark as fraud, update models |

## 8. Data Models

### 8.1 Risk Evaluation
```typescript
{
  evaluationId: string;
  paymentId: string;
  merchantId: string;
  customerId: string;
  riskScore: number; // 0-100
  riskDecision: RiskDecision; // APPROVE, REJECT, REVIEW
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

### 8.2 Risk Rule
```typescript
{
  ruleId: string;
  ruleName: string;
  ruleType: RuleType; // VELOCITY, BLACKLIST, AMOUNT, GEO, ML_MODEL
  version: number;
  isActive: boolean;
  priority: number;
  conditions: RuleCondition[];
  action: RuleAction; // BLOCK, FLAG, ADD_SCORE
  scoreImpact: number;
  createdAt: DateTime;
  updatedAt: DateTime;
}
```

### 8.3 Merchant Risk Profile
```typescript
{
  profileId: string;
  merchantId: string;
  riskLevel: RiskLevel; // LOW, MEDIUM, HIGH
  riskThresholds: {
    autoRejectThreshold: number;
    manualReviewThreshold: number;
  };
  enabledRules: string[];
  whitelistedCountries: string[];
  blacklistedCountries: string[];
  maxTransactionAmount: Money;
  velocityLimits: VelocityLimit[];
  customRules: CustomRuleConfig[];
}
```

## 9. Acceptance Criteria

### 9.1 Risk Evaluation
- [ ] Evaluations complete within 500ms
- [ ] Same payment produces same decision
- [ ] Risk scores are 0-100
- [ ] High-risk payments are rejected
- [ ] All triggered rules are logged

### 9.2 Rule Engine
- [ ] Rules are evaluated by priority
- [ ] Rule versions are tracked
- [ ] Inactive rules are not evaluated
- [ ] Rule performance is measured

### 9.3 Merchant Profiles
- [ ] Merchant profiles are isolated
- [ ] Profile changes are audited
- [ ] Profiles are cached for performance

### 9.4 Velocity Checking
- [ ] Velocity limits are enforced
- [ ] Multiple windows are supported
- [ ] Exceeded velocity flags transaction

## 10. Dependencies

### 10.1 Upstream Dependencies
- Payment Processing Service
- Merchant Service (for profiles)

### 10.2 Downstream Dependencies
- Payment Processing Service (decision response)
- Analytics Service (evaluation history)
- Alerting Service (suspicious activity)

### 10.3 Third-Party Dependencies
- ML model scoring services (optional)
- Fraud intelligence providers (optional)
- GeoIP services

## 11. Database Schema

### 11.1 Tables
- `risk_rules` - Rule definitions
- `risk_evaluations` - Evaluation results
- `merchant_risk_profiles` - Merchant-specific config
- `risk_evaluation_history` - Historical log
- `suspicious_patterns` - Detected fraud patterns

### 11.2 Data Patterns
- **Write:** High frequency (every payment)
- **Read:** Frequent (evaluation queries)
- **Hot data:** Recent evaluations (last 30 days)
- **Cold data:** Historical evaluations for ML training

## 12. Risks & Assumptions

### 12.1 Risks
- **RISK-1:** False positives blocking legitimate payments - Mitigation: Tunable thresholds, whitelist support
- **RISK-2:** False negatives allowing fraud - Mitigation: ML model integration, feedback loop
- **RISK-3:** Rule engine complexity - Mitigation: Rule versioning, testing framework

### 12.2 Assumptions
- Payment details are available for evaluation
- Merchant profiles are pre-configured
- GeoIP service is available
- ML model services are available (if used)

## 13. Success Metrics

- **Metric 1:** Risk evaluation p95 latency < 500ms
- **Metric 2:** Fraud detection rate > 90%
- **Metric 3:** False positive rate < 5%
- **Metric 4:** Chargeback reduction > 50%
- **Metric 5:** Rule evaluation time < 100ms
