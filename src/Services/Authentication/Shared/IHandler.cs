namespace Authentication.Shared;

/// <summary>
/// Base interface for command/query handlers
/// </summary>
public interface IHandler<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
