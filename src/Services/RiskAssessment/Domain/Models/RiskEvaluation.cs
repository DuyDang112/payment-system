using RiskAssessment.Domain.Events;

namespace RiskAssessment.Domain.Models;

/// <summary>
/// Root aggregate for risk evaluations
/// </summary>
public sealed class RiskEvaluation
{
    private readonly List<RuleTrigger> _triggeredRules = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    private RiskEvaluation()
    {
        // For EF Core
    }

    public Guid Id { get; private set; }
    public string EvaluationId { get; private set; } = string.Empty;
    public string PaymentId { get; private set; } = string.Empty;
    public string MerchantId { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public int RiskScore { get; private set; }
    public string Decision { get; private set; } = string.Empty;
    public string RuleVersion { get; private set; } = string.Empty;
    public DateTime EvaluatedAt { get; private set; }
    public decimal? Amount { get; private set; }
    public string? Currency { get; private set; }
    public string? CountryCode { get; private set; }
    public string? IpAddress { get; private set; }
    public string? CustomerEmail { get; private set; }

    public IReadOnlyCollection<RuleTrigger> TriggeredRules => _triggeredRules.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Factory method to create a new risk evaluation
    /// </summary>
    public static RiskEvaluation Create(
        string paymentId,
        string merchantId,
        string customerId,
        decimal? amount,
        string? currency,
        string? countryCode,
        string? ipAddress,
        string? customerEmail)
    {
        var evaluation = new RiskEvaluation
        {
            Id = Guid.NewGuid(),
            EvaluationId = Guid.NewGuid().ToString("N"),
            PaymentId = paymentId,
            MerchantId = merchantId,
            CustomerId = customerId,
            Amount = amount,
            Currency = currency,
            CountryCode = countryCode,
            IpAddress = ipAddress,
            CustomerEmail = customerEmail,
            RiskScore = 0,
            Decision = RiskDecision.APPROVE.ToString(),
            RuleVersion = "1.0",
            EvaluatedAt = DateTime.UtcNow
        };

        return evaluation;
    }

    /// <summary>
    /// Add a triggered rule to the evaluation
    /// </summary>
    public void AddTriggeredRule(RuleTrigger trigger)
    {
        _triggeredRules.Add(trigger);
    }

    /// <summary>
    /// Calculate and set the final risk score
    /// </summary>
    public void CalculateRiskScore(List<RuleTrigger> triggers)
    {
        int totalScore = triggers.Sum(t => t.ScoreImpact);
        RiskScore = Math.Min(100, Math.Max(0, totalScore)); // Clamp between 0-100
    }

    /// <summary>
    /// Make risk decision based on score and merchant profile thresholds
    /// </summary>
    public void MakeDecision(RiskScore score, MerchantRiskProfile profile)
    {
        Decision = score.Value switch
        {
            var s when s >= profile.AutoRejectThreshold => RiskDecision.REJECT.ToString(),
            var s when s >= profile.ManualReviewThreshold => RiskDecision.REVIEW.ToString(),
            _ => RiskDecision.APPROVE.ToString()
        };
    }

    /// <summary>
    /// Set the final risk score and decision
    /// </summary>
    public void SetFinalResult(int score, RiskDecision decision, string ruleVersion)
    {
        RiskScore = Math.Min(100, Math.Max(0, score));
        Decision = decision.ToString();
        RuleVersion = ruleVersion;
    }

    /// <summary>
    /// Add a domain event
    /// </summary>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clear domain events after publishing
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
