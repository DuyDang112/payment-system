namespace RiskAssessment.Domain.Models;

/// <summary>
/// Merchant-specific risk profile configuration
/// </summary>
public sealed class MerchantRiskProfile
{
    private readonly List<string> _enabledRuleIds = new();
    private readonly List<string> _whitelistedCountries = new();
    private readonly List<string> _blacklistedCountries = new();
    private readonly List<VelocityLimit> _velocityLimits = new();

    private MerchantRiskProfile()
    {
        // For EF Core
    }

    public Guid Id { get; private set; }
    public string MerchantId { get; private set; } = string.Empty;
    public string RiskLevel { get; private set; } = string.Empty;
    public int AutoRejectThreshold { get; private set; } = 80;
    public int ManualReviewThreshold { get; private set; } = 50;
    public decimal? MaxTransactionAmount { get; private set; }
    public string? MaxTransactionCurrency { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<string> EnabledRuleIds => _enabledRuleIds.AsReadOnly();
    public IReadOnlyCollection<string> WhitelistedCountries => _whitelistedCountries.AsReadOnly();
    public IReadOnlyCollection<string> BlacklistedCountries => _blacklistedCountries.AsReadOnly();
    public IReadOnlyCollection<VelocityLimit> VelocityLimits => _velocityLimits.AsReadOnly();

    /// <summary>
    /// Factory method to create a new merchant profile
    /// </summary>
    public static MerchantRiskProfile Create(
        string merchantId,
        RiskLevel riskLevel = global::RiskAssessment.Domain.Models.RiskLevel.MEDIUM)
    {
        var now = DateTime.UtcNow;

        var profile = new MerchantRiskProfile
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId,
            RiskLevel = riskLevel.ToString(),
            AutoRejectThreshold = 80,
            ManualReviewThreshold = 50,
            CreatedAt = now,
            UpdatedAt = now
        };

        return profile;
    }

    /// <summary>
    /// Update risk thresholds
    /// </summary>
    public void UpdateThresholds(int autoReject, int manualReview)
    {
        if (autoReject < 0 || autoReject > 100)
            throw new ArgumentException("Auto reject threshold must be between 0 and 100", nameof(autoReject));

        if (manualReview < 0 || manualReview > 100)
            throw new ArgumentException("Manual review threshold must be between 0 and 100", nameof(manualReview));

        if (manualReview >= autoReject)
            throw new ArgumentException("Manual review threshold must be less than auto reject threshold");

        AutoRejectThreshold = autoReject;
        ManualReviewThreshold = manualReview;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enable a rule for this merchant
    /// </summary>
    public void EnableRule(string ruleId)
    {
        if (!_enabledRuleIds.Contains(ruleId))
        {
            _enabledRuleIds.Add(ruleId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Disable a rule for this merchant
    /// </summary>
    public void DisableRule(string ruleId)
    {
        _enabledRuleIds.Remove(ruleId);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add a whitelisted country
    /// </summary>
    public void AddWhitelistedCountry(string countryCode)
    {
        if (!_whitelistedCountries.Contains(countryCode))
        {
            _whitelistedCountries.Add(countryCode);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Remove a whitelisted country
    /// </summary>
    public void RemoveWhitelistedCountry(string countryCode)
    {
        _whitelistedCountries.Remove(countryCode);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add a blacklisted country
    /// </summary>
    public void AddBlacklistedCountry(string countryCode)
    {
        if (!_blacklistedCountries.Contains(countryCode))
        {
            _blacklistedCountries.Add(countryCode);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Remove a blacklisted country
    /// </summary>
    public void RemoveBlacklistedCountry(string countryCode)
    {
        _blacklistedCountries.Remove(countryCode);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set or update a velocity limit
    /// </summary>
    public void SetVelocityLimit(VelocityLimit limit)
    {
        var existing = _velocityLimits.FirstOrDefault(v => v.WindowType == limit.WindowType);
        if (existing != null)
        {
            _velocityLimits.Remove(existing);
        }
        _velocityLimits.Add(limit);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set maximum transaction amount
    /// </summary>
    public void SetMaxTransactionAmount(decimal? amount, string? currency)
    {
        MaxTransactionAmount = amount;
        MaxTransactionCurrency = currency;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update risk level
    /// </summary>
    public void UpdateRiskLevel(RiskLevel riskLevel)
    {
        RiskLevel = riskLevel.ToString();
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Velocity limit configuration
/// </summary>
public sealed record VelocityLimit(
    string WindowType,
    int Limit,
    string? Description);
