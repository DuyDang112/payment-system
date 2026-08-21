using Authentication.Domain;
using Authentication.Infrastructure.Data;
using Authentication.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Authentication.Features.UserInfo;

/// <summary>
/// Handler for user info requests
/// </summary>
public class UserInfoHandler : IUserInfoHandler
{
    private readonly AuthenticationDbContext _dbContext;
    private readonly ILogger<UserInfoHandler> _logger;

    public UserInfoHandler(
        AuthenticationDbContext dbContext,
        ILogger<UserInfoHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UserInfoResponse> HandleAsync(UserInfoRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                _logger.LogWarning("UserInfo request failed: No user ID provided");
                return UserInfoResponse.Failed("invalid_token");
            }

            if (!Guid.TryParse(request.UserId, out var userId))
            {
                _logger.LogWarning("UserInfo request failed: Invalid user ID format - {UserId}", request.UserId);
                return UserInfoResponse.Failed("invalid_token");
            }

            _logger.LogInformation("Processing UserInfo request for user: {UserId}", userId);

            // Find user with related data
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("UserInfo request failed: User not found - {UserId}", userId);
                return UserInfoResponse.Failed("user_not_found");
            }

            // Extract roles
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Extract custom claims
            var claims = new Dictionary<string, string>();
            foreach (var claim in user.Claims)
            {
                claims[claim.ClaimType] = claim.ClaimValue;
            }

            _logger.LogInformation("UserInfo request successful for user: {UserId}", userId);

            return UserInfoResponse.Successful(
                user.Id.ToString(),
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                user.EmailVerified,
                roles,
                claims
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserInfo request for user: {UserId}", request.UserId);
            return UserInfoResponse.Failed("server_error");
        }
    }
}
