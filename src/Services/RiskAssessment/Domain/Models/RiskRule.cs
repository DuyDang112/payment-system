namespace RiskAssessment.Domain.Models;

/// <summary>
/// Risk rule entity with versioning support
/// </summary>
public sealed class RiskRule
{
    private RiskRule()
    {
        // For EF Core
    }

    public Guid Id { get; private set; }
    public string RuleId { get; private set; } = string.Empty;
    public string RuleName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string RuleType { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public int ScoreImpact { get; private set; }
    public string ConditionsJson { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Factory method to create a new rule (version 1)
    /// </summary>
    public static RiskRule Create(
        string ruleName,
        string description,
        RuleType ruleType,
        int priority,
        RuleAction action,
        int scoreImpact,
        string conditionsJson)
    {
        var ruleId = GenerateRuleId();
        var now = DateTime.UtcNow;

        return new RiskRule
        {
            Id = Guid.NewGuid(),
            RuleId = ruleId,
            RuleName = ruleName,
            Description = description,
            RuleType = ruleType.ToString(),
            Version = 1,
            IsActive = true,
            Priority = priority,
            Action = action.ToString(),
            ScoreImpact = scoreImpact,
            ConditionsJson = conditionsJson,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Create a new version of this rule (deactivates current)
    /// </summary>
    public RiskRule CreateNewVersion(
        string ruleName,
        string description,
        int priority,
        RuleAction action,
        int scoreImpact,
        string conditionsJson)
    {
        // Deactivate current version
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        // Create new version
        var newVersion = new RiskRule
        {
            Id = Guid.NewGuid(),
            RuleId = RuleId,
            RuleName = ruleName,
            Description = description,
            RuleType = RuleType,
            Version = Version + 1,
            IsActive = true,
            Priority = priority,
            Action = action.ToString(),
            ScoreImpact = scoreImpact,
            ConditionsJson = conditionsJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return newVersion;
    }

    /// <summary>
    /// Deactivate the rule
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activate the rule
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft delete the rule
    /// </summary>
    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update rule properties
    /// </summary>
    public void Update(
        string ruleName,
        string description,
        int priority,
        RuleAction action,
        int scoreImpact,
        string conditionsJson)
    {
        RuleName = ruleName;
        Description = description;
        Priority = priority;
        Action = action.ToString();
        ScoreImpact = scoreImpact;
        ConditionsJson = conditionsJson;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if rule is active at specific version
    /// </summary>
    public bool IsActiveAtVersion(int version)
    {
        return IsActive && Version == version;
    }

    private static string GenerateRuleId()
    {
        return $"RULE-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
