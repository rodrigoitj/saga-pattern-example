namespace Shared.Domain.Abstractions;

/// <summary>
/// Represents a generic result that can be either a success or a failure.
/// Implements the Result Pattern for better error handling.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the Result class.
    /// </summary>
    /// <param name="isSuccess">A value indicating whether the result is successful.</param>
    /// <param name="error">The error message if the result is a failure.</param>
    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the result is successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if the result is a failure; otherwise, null.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful Result instance.</returns>
    public static Result Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed Result instance.</returns>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="value">The result value.</param>
    /// <returns>A successful Result&lt;T&gt; instance.</returns>
    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="error">The error message.</param>
    /// <returns>A failed Result&lt;T&gt; instance.</returns>
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

/// <summary>
/// Represents a generic result with a value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the Result&lt;T&gt; class.
/// </remarks>
/// <param name="value">The result value.</param>
/// <param name="isSuccess">A value indicating whether the result is successful.</param>
/// <param name="error">The error message if the result is a failure.</param>
public class Result<T>(T? value, bool isSuccess, string error) : Result(isSuccess, error)
{
    /// <summary>
    /// Gets the result value if the result is successful; otherwise, null.
    /// </summary>
    public T? Value { get; } = value;

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The result value.</param>
    /// <returns>A successful Result&lt;T&gt; instance.</returns>
    public static Result<T> Success(T value) => new(value, true, string.Empty);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed Result&lt;T&gt; instance.</returns>
    public static new Result<T> Failure(string error) => new(default, false, error);
}
