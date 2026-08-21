using Authentication.Shared;

namespace Authentication.Features.Register;

/// <summary>
/// Interface for Register Handler
/// </summary>
public interface IRegisterHandler : IHandler<RegisterRequest, RegisterResponse>
{
}
