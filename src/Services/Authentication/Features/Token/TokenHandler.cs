using Authentication.Shared;
using Microsoft.Extensions.Logging;

namespace Authentication.Features.Token;

/// <summary>
/// Handler for token operations (simplified JWT approach)
/// </summary>
public class TokenHandler : ITokenHandler
{
    private readonly ILogger<TokenHandler> _logger;

    public TokenHandler(ILogger<TokenHandler> logger)
    {
        _logger = logger;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing token request for grant type: {GrantType}", request.GrantType);

            // For simple JWT authentication, token operations are handled through the Login endpoint
            // This endpoint is maintained for compatibility but delegates to the main login flow

            _logger.LogInformation("Token requests should use /api/auth/login endpoint instead");
            return await Task.FromResult(TokenResponse.Failed("use_login_endpoint", "Please use /api/auth/login endpoint for authentication"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing token request for grant type: {GrantType}", request.GrantType);
            return await Task.FromResult(TokenResponse.Failed("server_error", "An error occurred while processing the token request"));
        }
    }
}