namespace PaymentRouter.Shared;

/// <summary>
/// Represents an error in the system
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "Null value provided");
    public static readonly Error Validation = new("Error.Validation", "Validation error occurred");
    public static readonly Error NotFound = new("Error.NotFound", "Resource not found");
    public static readonly Error Conflict = new("Error.Conflict", "Resource conflict occurred");
    public static readonly Error Failed = new("Error.Failed", "Operation failed");
}
