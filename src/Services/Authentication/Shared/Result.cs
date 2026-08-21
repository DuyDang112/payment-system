namespace Authentication.Shared;

/// <summary>
/// Generic result type for operations
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public Error? Error { get; }
    public string? Message { get; }

    private Result(bool isSuccess, T? data, Error? error, string? message)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
        Message = message;
    }

    public static Result<T> Success(T data, string? message = null)
        => new Result<T>(true, data, null, message);

    public static Result<T> Failure(Error error, string? message = null)
        => new Result<T>(false, default, error, message);

    public static Result<T> Failure(string errorCode, string errorMessage)
        => Failure(new Error(errorCode, errorMessage));
}

/// <summary>
/// Error representation
/// </summary>
public class Error
{
    public string Code { get; }
    public string Message { get; }

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
