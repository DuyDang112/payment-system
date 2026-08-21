using Authentication.Shared;
using Microsoft.Extensions.Logging;

namespace Authentication.Features.Logout;

/// <summary>
/// Handler for logout operations
/// </summary>
public class LogoutHandler : ILogoutHandler
{
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(ILogger<LogoutHandler> logger)
    {
        _logger = logger;
    }

    public async Task<LogoutResponse> HandleAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing logout request");

            // In a full implementation, you would:
            // 1. Invalidate the refresh token if provided
            // 2. Add the access token to a denylist/token revocation list
            // 3. Log the logout event for security auditing
            // 4. Optionally notify other services about the logout

            if (!string.IsNullOrEmpty(request.RefreshToken))
            {
                // TODO: Invalidate refresh token in the database
                _logger.LogInformation("Invalidating refresh token");
            }

            _logger.LogInformation("Logout successful");

            return await Task.FromResult(LogoutResponse.Successful(request.PostLogoutRedirectUri));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return LogoutResponse.Failed("server_error", "An error occurred during logout");
        }
    }
}
