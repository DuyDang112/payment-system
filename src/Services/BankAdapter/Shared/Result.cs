namespace BankAdapter.Shared;

/// <summary>
/// Represents the result of an operation that can either succeed or fail
/// </summary>
public sealed class Result
{
    public bool IsSuccess { get; }
    public bool IsError => !IsSuccess;
    public List<Error> Errors { get; }

    private Result(bool isSuccess, List<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new Result(true, new());
    public static Result Failure(Error error) => new Result(false, new() { error });
    public static Result Failure(List<Error> errors) => new Result(false, errors);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsError => !IsSuccess;
    public T? Value { get; }
    public List<Error> Errors { get; }

    private Result(bool isSuccess, T? value, List<Error> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new Result<T>(true, value, new());
    public static Result<T> Failure(Error error) => new Result<T>(false, default, new() { error });
    public static Result<T> Failure(List<Error> errors) => new Result<T>(false, default, errors);

    public static implicit operator Result<T>(Error error) => Failure(error);
    public static implicit operator Result<T>(T value) => Success(value);
}
