namespace ForEach.Conduit;

/// <summary>
/// Represents the result of an operation, which can be either a success or a failure.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets the error associated with the failure, or <c>null</c> if the result is a success.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// </summary>
    public bool IsSuccess => Error == null;

    private Result() => Error = null;

    private Result(
        Error error) => Error = error;

    /// <summary>
    /// Returns a successful result.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new();

    /// <summary>
    /// Returns a failed result.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(
        Error error) => new(error);

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if this result is a failure.
    /// </summary>
    public void ThrowIfFailure()
    {
        if (!IsSuccess)
            throw new InvalidOperationException($"Result is a failure: {Error}");
    }
}

/// <summary>
/// Represents the result of an operation that returns a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Gets the value associated with a successful result, or <c>null</c> if the result is a failure.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error associated with a failure, or <c>null</c> if the result is a success.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// </summary>
    public bool IsSuccess => Error == null;

    private Result(
        T value) => Value = value;

    private Result(
        Error error) => Error = error;

    /// <summary>
    /// Returns a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Success(
        T value) => new(value);

    /// <summary>
    /// Returns a failed result.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure(
        Error error) => new(error);

    /// <summary>
    /// Returns the value, or throws <see cref="InvalidOperationException"/> if this result is a failure.
    /// </summary>
    public T GetValueOrThrow() =>
        IsSuccess
            ? Value!
            : throw new InvalidOperationException($"Cannot access value of a failed result: {Error}");

    /// <summary>
    /// Returns the value if successful, or <paramref name="fallback"/> if not.
    /// </summary>
    /// <param name="fallback">The fallback value.</param>
    /// <returns>The value if successful; otherwise, the fallback value.</returns>
    public T GetValueOrDefault(
        T fallback) => IsSuccess ? Value! : fallback;
}