using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RiskAssessment.Domain.Events;
using RiskAssessment.Domain.Models;
using RiskAssessment.Features.Shared.Errors;
using RiskAssessment.Infrastructure.Data;
using RiskAssessment.Infrastructure.RuleEngine;
using RiskAssessment.Infrastructure.VelocityChecking;
using RiskAssessment.Shared;
using Shared;
using Shared.Observability;
using System.Diagnostics;
using System.Text.Json;

namespace RiskAssessment.Features.EvaluateRisk;

/// <summary>
/// Handler for risk evaluation
/// </summary>
internal sealed class EvaluateRiskHandler(
    RiskAssessmentDbContext context,
    IRuleEngine ruleEngine,
    IVelocityChecker velocityChecker,
    ILogger<EvaluateRiskHandler> logger) : IEvaluateRiskHandler
{
    public async Task<Result<EvaluateRiskResponse>> HandleAsync(
        EvaluateRiskRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation(
                "Starting risk evaluation for payment {PaymentId} for merchant {MerchantId}",
                request.PaymentId,
                request.MerchantId);

            // Check whitelist first
            var whitelistCheck = await CheckWhitelistAsync(request, cancellationToken);
            if (whitelistCheck.IsWhitelisted)
            {
                logger.LogInformation(
                    "Customer {CustomerId} is whitelisted - auto-approving",
                    request.CustomerId);

                var whitelistedResponse = CreateApprovedResponse(
                    request,
                    "Customer is whitelisted",
                    new List<RuleTrigger>());

                return Result.Success(whitelistedResponse);
            }

            // Check blacklist
            var blacklistCheck = await CheckBlacklistAsync(request, cancellationToken);
            if (blacklistCheck.IsBlacklisted)
            {
                logger.LogWarning(
                    "Customer {CustomerId} is blacklisted - rejecting",
                    request.CustomerId);

                stopwatch.Stop();
                MetricHelper.RecordRiskRejection("blacklist");

                return Result<EvaluateRiskResponse>.Failure(RiskErrors.EntityBlacklisted);
            }

            // Get merchant profile
            var profile = await context.MerchantRiskProfiles
                .FirstOrDefaultAsync(p => p.MerchantId == request.MerchantId, cancellationToken);

            if (profile == null)
            {
                logger.LogWarning(
                    "Merchant profile not found for {MerchantId}",
                    request.MerchantId);

                return Result<EvaluateRiskResponse>.Failure(RiskErrors.MerchantProfileNotFoundWithId(request.MerchantId));
            }

            // Check velocity limits
            await CheckVelocityLimitsAsync(request, profile, cancellationToken);

            // Run rule engine
            var ruleResult = await ruleEngine.EvaluateAsync(request, profile, cancellationToken);

            // Create evaluation
            var evaluation = RiskEvaluation.Create(
                request.PaymentId,
                request.MerchantId,
                request.CustomerId,
                request.Amount,
                request.Currency,
                request.CountryCode,
                request.IpAddress,
                request.CustomerEmail);

            // Add triggered rules
            foreach (var trigger in ruleResult.TriggeredRules)
            {
                evaluation.AddTriggeredRule(trigger);
            }

            // Make decision
            var riskScore = RiskScore.Of(ruleResult.TotalScore);
            evaluation.MakeDecision(riskScore, profile);

            // If rule engine says block, override decision
            if (ruleResult.ShouldBlock)
            {
                evaluation.SetFinalResult(ruleResult.TotalScore, RiskDecision.REJECT, "1.0");
            }

            evaluation.SetFinalResult(ruleResult.TotalScore,
                Enum.Parse<RiskDecision>(evaluation.Decision),
                "1.0");

            // Save evaluation
            await context.RiskEvaluations.AddAsync(evaluation, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Risk evaluation completed: {EvaluationId}, Score: {Score}, Decision: {Decision}",
                evaluation.EvaluationId,
                evaluation.RiskScore,
                evaluation.Decision);

            // Record risk rejection metric if decision is REJECT
            if (evaluation.Decision == "REJECT")
            {
                stopwatch.Stop();
                var reason = ruleResult.TriggeredRules.FirstOrDefault()?.RuleName ?? "high_risk_score";
                MetricHelper.RecordRiskRejection(reason);
            }

            // Add domain event
            evaluation.AddDomainEvent(new RiskEvaluationCompletedEvent(
                evaluation.EvaluationId,
                evaluation.PaymentId,
                evaluation.MerchantId,
                evaluation.RiskScore,
                Enum.Parse<RiskDecision>(evaluation.Decision),
                evaluation.EvaluatedAt));

            var response = evaluation.MapToResponse();
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during risk evaluation for payment {PaymentId}", request.PaymentId);
            return Result.Failure<EvaluateRiskResponse>(new Error(
                "Risk.EvaluationError",
                $"Error during risk evaluation: {ex.Message}"));
        }
    }

    private async Task<(bool IsWhitelisted, string? Reason)> CheckWhitelistAsync(
        EvaluateRiskRequest request,
        CancellationToken cancellationToken)
    {
        // Check if customer is whitelisted
        var customerWhitelist = await context.WhitelistEntries
            .AnyAsync(w =>
                w.EntityType == WhitelistEntry.EntityTypeCustomer &&
                w.EntityValue == request.CustomerId,
                cancellationToken);

        if (customerWhitelist)
        {
            return (true, "Customer is whitelisted");
        }

        // Check if IP is whitelisted
        if (!string.IsNullOrEmpty(request.IpAddress))
        {
            var ipWhitelist = await context.WhitelistEntries
                .AnyAsync(w =>
                    w.EntityType == WhitelistEntry.EntityTypeIp &&
                    w.EntityValue == request.IpAddress,
                    cancellationToken);

            if (ipWhitelist)
            {
                return (true, "IP address is whitelisted");
            }
        }

        return (false, null);
    }

    private async Task<(bool IsBlacklisted, string? Reason)> CheckBlacklistAsync(
        EvaluateRiskRequest request,
        CancellationToken cancellationToken)
    {
        // Check if customer is blacklisted
        var customerBlacklist = await context.BlacklistEntries
            .AnyAsync(b =>
                b.EntityType == BlacklistEntry.EntityTypeCustomer &&
                b.EntityValue == request.CustomerId,
                cancellationToken);

        if (customerBlacklist)
        {
            return (true, "Customer is blacklisted");
        }

        // Check if IP is blacklisted
        if (!string.IsNullOrEmpty(request.IpAddress))
        {
            var ipBlacklist = await context.BlacklistEntries
                .AnyAsync(b =>
                    b.EntityType == BlacklistEntry.EntityTypeIp &&
                    b.EntityValue == request.IpAddress,
                    cancellationToken);

            if (ipBlacklist)
            {
                return (true, "IP address is blacklisted");
            }
        }

        // Check if email is blacklisted
        if (!string.IsNullOrEmpty(request.CustomerEmail))
        {
            var emailBlacklist = await context.BlacklistEntries
                .AnyAsync(b =>
                    b.EntityType == BlacklistEntry.EntityTypeEmail &&
                    b.EntityValue == request.CustomerEmail,
                    cancellationToken);

            if (emailBlacklist)
            {
                return (true, "Email is blacklisted");
            }
        }

        return (false, null);
    }

    private async Task CheckVelocityLimitsAsync(
        EvaluateRiskRequest request,
        MerchantRiskProfile profile,
        CancellationToken cancellationToken)
    {
        foreach (var limit in profile.VelocityLimits)
        {
            var result = await velocityChecker.CheckVelocityAsync(
                request.CustomerId,
                request.MerchantId,
                limit.WindowType,
                limit.Limit,
                cancellationToken);

            if (result.IsExceeded)
            {
                logger.LogWarning(
                    "Velocity limit exceeded: {WindowType} - {Message}",
                    limit.WindowType,
                    result.Message);
            }

            // Increment counter
            await velocityChecker.IncrementCountAsync(
                request.CustomerId,
                request.MerchantId,
                limit.WindowType,
                cancellationToken);
        }
    }

    private static EvaluateRiskResponse CreateApprovedResponse(
        EvaluateRiskRequest request,
        string reason,
        List<RuleTrigger> triggeredRules)
    {
        return new EvaluateRiskResponse
        {
            EvaluationId = Guid.NewGuid().ToString("N"),
            PaymentId = request.PaymentId,
            RiskScore = 0,
            Decision = RiskDecision.APPROVE.ToString(),
            EvaluatedAt = DateTime.UtcNow,
            RuleVersion = "1.0",
            TriggeredRules = triggeredRules.Select(t => new TriggeredRuleDto
            {
                RuleId = t.RuleId,
                RuleName = t.RuleName,
                RuleType = t.RuleType.ToString(),
                Action = t.Action.ToString(),
                ScoreImpact = t.ScoreImpact,
                Description = t.Description
            }).ToList(),
            Warning = reason
        };
    }
}
