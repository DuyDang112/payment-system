using Authentication.Shared;

namespace Authentication.Features.Token;

/// <summary>
/// Interface for Token Handler
/// </summary>
public interface ITokenHandler : IHandler<TokenRequest, TokenResponse>
{
}
