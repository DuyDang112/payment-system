namespace PaymentRouter.Shared;

/// <summary>
/// Result pattern for error handling without exceptions
/// </summary>
public sealed record Result
{
    public bool IsSuccess { get; }
    public bool IsError => !IsSuccess;
    public Error[] Errors { get; }

    private Result(bool isSuccess, Error[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new(true, Array.Empty<Error>());

    public static Result Failure(Error error) => new(false, new[] { error });

    public static Result Failure(Error[] errors) => new(false, errors);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    public static Result<T> Failure<T>(Error[] errors) => Result<T>.Failure(errors);
}

/// <summary>
/// Result pattern with value for error handling without exceptions
/// </summary>
public sealed record Result<T>
{
    public bool IsSuccess { get; }
    public bool IsError => !IsSuccess;
    public T? Value { get; }
    public Error[] Errors { get; }

    private Result(bool isSuccess, T? value, Error[] errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<Error>());

    public static Result<T> Failure(Error error) => new(false, default, new[] { error });

    public static Result<T> Failure(Error[] errors) => new(false, default, errors);

    public static async Task<Result<T>> SuccessAsync(T value) => await Task.FromResult(Success(value));

    public static async Task<Result<T>> FailureAsync(Error error) => await Task.FromResult(Failure(error));

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Error[], TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Errors);
    }
}
