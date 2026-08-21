using Authentication.Domain;
using Authentication.Infrastructure.Data;
using Authentication.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Authentication.Features.Register;

/// <summary>
/// Handler for user registration operations
/// </summary>
public class RegisterHandler : IRegisterHandler
{
    private readonly AuthenticationDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(
        AuthenticationDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        ILogger<RegisterHandler> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<RegisterResponse> HandleAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing registration request for user: {Username}", request.Username);

            // Check if username already exists
            var existingUsername = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower(), cancellationToken);

            if (existingUsername != null)
            {
                _logger.LogWarning("Registration failed: Username already exists - {Username}", request.Username);
                return RegisterResponse.Failed("username_exists", "Username is already taken");
            }

            // Check if email already exists
            var existingEmail = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

            if (existingEmail != null)
            {
                _logger.LogWarning("Registration failed: Email already exists - {Email}", request.Email);
                return RegisterResponse.Failed("email_exists", "Email address is already registered");
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Hash password
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            // Assign default role (you may want to make this configurable)
            var defaultRole = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);

            if (defaultRole != null)
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = defaultRole.Id,
                    AssignedAt = DateTime.UtcNow
                });
            }

            // Add user to database
            await _dbContext.Users.AddAsync(user, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Registration successful for user: {Username}, UserId: {UserId}",
                request.Username, user.Id);

            return RegisterResponse.Successful(user.Id, user.Username, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user: {Username}", request.Username);
            return RegisterResponse.Failed("server_error", "An error occurred during registration");
        }
    }
}
