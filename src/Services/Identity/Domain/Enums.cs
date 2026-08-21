namespace Identity.Domain;

/// <summary>
/// Represents the grant types supported by the Identity Server
/// </summary>
public enum GrantType
{
    /// <summary>
    /// Authorization code flow for user to app authentication
    /// </summary>
    AuthorizationCode = 0,

    /// <summary>
    /// Client credentials flow for app to app authentication
    /// </summary>
    ClientCredentials = 1,

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    RefreshToken = 2,

    /// <summary>
    /// Resource owner password flow (discouraged, use only for legacy scenarios)
    /// </summary>
    ResourceOwnerPassword = 3
}

/// <summary>
/// Represents the token types issued by the Identity Server
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Access token for API access
    /// </summary>
    AccessToken = 0,

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    RefreshToken = 1,

    /// <summary>
    /// Identity token for user information
    /// </summary>
    IdentityToken = 2,

    /// <summary>
    /// Authorization code for exchanging tokens
    /// </summary>
    AuthorizationCode = 3
}

/// <summary>
/// Represents the status of a client
/// </summary>
public enum ClientStatus
{
    /// <summary>
    /// Client is active and can request tokens
    /// </summary>
    Active = 0,

    /// <summary>
    /// Client is disabled and cannot request tokens
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// Client is revoked and all tokens are invalidated
    /// </summary>
    Revoked = 2
}
