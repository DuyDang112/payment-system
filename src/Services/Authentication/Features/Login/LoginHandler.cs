using Authentication.Domain;
using Authentication.Infrastructure.Data;
using Authentication.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Authentication.Features.Login;

/// <summary>
/// Handler for login operations
/// </summary>
public class LoginHandler : ILoginHandler
{
    private readonly AuthenticationDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        AuthenticationDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<LoginHandler> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing login request for user: {Username}", request.Username);

            // Find user by username (email)
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.Claims)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found - {Username}", request.Username);
                return LoginResponse.Failed("invalid_credentials", "Invalid username or password");
            }

            // Verify password
            var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (passwordResult != PasswordVerificationResult.Success)
            {
                _logger.LogWarning("Login failed: Invalid password for user - {Username}", request.Username);
                return LoginResponse.Failed("invalid_credentials", "Invalid username or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: User account is disabled - {Username}", request.Username);
                return LoginResponse.Failed("account_disabled", "Your account has been disabled");
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Generate access token using JWT
            var accessToken = GenerateAccessToken(user);

            _logger.LogInformation("Login successful for user: {Username}, UserId: {UserId}", request.Username, user.Id);

            return LoginResponse.Successful(accessToken, expiresIn: 3600); // 1 hour expiry
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
            return LoginResponse.Failed("server_error", "An error occurred during login");
        }
    }

    private string GenerateAccessToken(User user)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return _jwtTokenService.GenerateToken(user.Id, user.Email, roles);
    }

    private string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString("N") + "-" + Guid.NewGuid().ToString("N");
    }
}
