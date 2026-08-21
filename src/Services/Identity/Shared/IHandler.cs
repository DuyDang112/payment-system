namespace Identity.Shared;

/// <summary>
/// Generic handler interface for processing requests and returning responses
/// </summary>
/// <typeparam name="TRequest">The type of request to handle</typeparam>
/// <typeparam name="TResponse">The type of response to return</typeparam>
public interface IHandler<in TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Handles the specified request and returns a response
    /// </summary>
    /// <param name="request">The request to handle</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the handler response</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler interface for processing requests without responses
/// </summary>
/// <typeparam name="TRequest">The type of request to handle</typeparam>
public interface IHandler<in TRequest>
    where TRequest : class
{
    /// <summary>
    /// Handles the specified request
    /// </summary>
    /// <param name="request">The request to handle</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the handler operation</returns>
    Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
