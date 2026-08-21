namespace Identity.Shared;

/// <summary>
/// Represents the result of an operation
/// </summary>
public class Result
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error if the operation failed
    /// </summary>
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result Success() => new Result(true);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static Result Failure(Error error) => new Result(false, error);
}

/// <summary>
/// Represents the result of an operation with a value
/// </summary>
public class Result<TValue> : Result
{
    /// <summary>
    /// Gets the result value
    /// </summary>
    public TValue? Value { get; }

    protected Result(TValue value) : base(true)
    {
        Value = value;
    }

    protected Result(Error error) : base(false, error)
    {
        Value = default;
    }

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    public static Result<TValue> Success(TValue value) => new Result<TValue>(value);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static new Result<TValue> Failure(Error error) => new Result<TValue>(error);
}
