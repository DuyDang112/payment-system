using Authentication.Shared;

namespace Authentication.Features.UserInfo;

/// <summary>
/// Interface for UserInfo Handler
/// </summary>
public interface IUserInfoHandler : IHandler<UserInfoRequest, UserInfoResponse>
{
}
