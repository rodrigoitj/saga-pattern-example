namespace Shared.Domain.Abstractions;

/// <summary>
/// Represents a generic result that can be either a success or a failure.
/// Implements the Result Pattern for better error handling.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

/// <summary>
/// Represents a generic result with a value.
/// </summary>
public class Result<T> : Result
{
    public Result(T? value, bool isSuccess, string error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, true, string.Empty);
    public static Result<T> Failure(string error) => new(default, false, error);
}
