using Authentication.Shared;

namespace Authentication.Features.Login;

/// <summary>
/// Interface for Login Handler
/// </summary>
public interface ILoginHandler : IHandler<LoginRequest, LoginResponse>
{
}
