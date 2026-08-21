namespace Authentication.Shared;

/// <summary>
/// Interface for API endpoints to enable auto-registration
/// </summary>
public interface IApiEndpoint
{
    void MapEndpoint(WebApplication app);
}
