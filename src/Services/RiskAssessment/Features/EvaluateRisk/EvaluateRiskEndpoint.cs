using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RiskAssessment.Domain.Models;
using RiskAssessment.Features.Shared.Routes;
using RiskAssessment.Shared;
using System.ComponentModel.DataAnnotations;

namespace RiskAssessment.Features.EvaluateRisk;

/// <summary>
/// API endpoint for risk evaluation
/// </summary>
public sealed class EvaluateRiskApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.BaseRoute + "/evaluate", Handle)
            .WithName("EvaluateRisk")
            .WithOpenApi()
            .WithTags("Risk Assessment")
            .WithSummary("Evaluate payment risk in real-time")
            .WithDescription("Evaluates payment risk using configurable business rules")
            .WithOpenApi(operation =>
            {
                operation.Description = "Evaluates payment risk and returns accept/reject/review decision based on configurable rules";
                return operation;
            });
    }

    private static async Task<IResult> Handle(
        [FromBody] EvaluateRiskRequest request,
        IValidator<EvaluateRiskRequest> validator,
        IEvaluateRiskHandler handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        // Use timeout to ensure < 500ms SLA
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        try
        {
            var response = await handler.HandleAsync(request, cts.Token);
            if (response.IsError)
            {
                return response.ToProblem();
            }

            return Results.Ok(response.Value);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            // Timeout - fail open as per BR-RS-007
            logger.LogWarning("Risk evaluation timed out for payment {PaymentId} - failing open", request.PaymentId);

            return Results.Ok(new EvaluateRiskResponse
            {
                EvaluationId = Guid.NewGuid().ToString("N"),
                PaymentId = request.PaymentId,
                RiskScore = 0,
                Decision = RiskDecision.APPROVE.ToString(),
                EvaluatedAt = DateTime.UtcNow,
                RuleVersion = "1.0",
                TimeoutOccurred = true,
                Warning = "Risk evaluation timed out, auto-approved per policy"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing risk evaluation for payment {PaymentId}", request.PaymentId);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Risk.EvaluationError",
                detail: "Error during risk evaluation");
        }
    }

    private static readonly ILogger<EvaluateRiskApiEndpoint> logger =
        LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<EvaluateRiskApiEndpoint>();
}
