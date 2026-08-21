using Authentication.Shared;

namespace Authentication.Features.Logout;

/// <summary>
/// Interface for Logout Handler
/// </summary>
public interface ILogoutHandler : IHandler<LogoutRequest, LogoutResponse>
{
}
