namespace Identity.Shared;

/// <summary>
/// Represents an error that occurred during an operation
/// </summary>
public class Error
{
    /// <summary>
    /// Gets the error code
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets additional error details
    /// </summary>
    public Dictionary<string, object>? Details { get; }

    public Error(string code, string message, Dictionary<string, object>? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }

    /// <summary>
    /// Creates a validation error
    /// </summary>
    public static Error Validation(string message, Dictionary<string, object>? details = null) =>
        new Error("VALIDATION_ERROR", message, details);

    /// <summary>
    /// Creates a not found error
    /// </summary>
    public static Error NotFound(string message, Dictionary<string, object>? details = null) =>
        new Error("NOT_FOUND", message, details);

    /// <summary>
    /// Creates an unauthorized error
    /// </summary>
    public static Error Unauthorized(string message, Dictionary<string, object>? details = null) =>
        new Error("UNAUTHORIZED", message, details);

    /// <summary>
    /// Creates a forbidden error
    /// </summary>
    public static Error Forbidden(string message, Dictionary<string, object>? details = null) =>
        new Error("FORBIDDEN", message, details);

    /// <summary>
    /// Creates an internal server error
    /// </summary>
    public static Error Internal(string message, Dictionary<string, object>? details = null) =>
        new Error("INTERNAL_ERROR", message, details);
}
