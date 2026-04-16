namespace ForEach.Conduit;

/// <summary>
/// A lightweight, allocation-free result type for operations that don't return a value.
/// </summary>
public readonly struct ValueResult
{
    /// <summary>
    /// The error associated with a failure, or <c>null</c> if the result is a success.
    /// </summary>
    public readonly Error? Error;

    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// </summary>
    public bool IsSuccess => Error == null;

    private ValueResult(
        Error? error) => Error = error;

    /// <summary>
    /// Returns a successful result.
    /// </summary>
    /// <returns>A successful <see cref="ValueResult"/>.</returns>
    public static ValueResult Success() => new(null);

    /// <summary>
    /// Returns a failed result.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed <see cref="ValueResult"/>.</returns>
    public static ValueResult Failure(
        Error error) => new(error);

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if this result is a failure.
    /// Use when you are certain the result is successful, or to make failure a programming error.
    /// </summary>
    public void ThrowIfFailure()
    {
        if (!IsSuccess)
            throw new InvalidOperationException($"Result is a failure: {Error}");
    }

    /// <summary>Entry point into the async extension method chain.</summary>
    public ValueTask<ValueResult> AsValueTask() => ValueTask.FromResult(this);
}

/// <summary>
/// A lightweight, allocation-free result type for operations that return a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public readonly struct ValueResult<T>
{
    /// <summary>
    /// The value associated with a successful result, or the default value of <typeparamref name="T"/> if the result is a failure.
    /// </summary>
    public readonly T? Value;

    /// <summary>
    /// The error associated with a failure, or <c>null</c> if the result is a success.
    /// </summary>
    public readonly Error? Error;

    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// </summary>
    public bool IsSuccess => Error == null;

    private ValueResult(
        T value)
    {
        Value = value;
        Error = null;
    }

    private ValueResult(
        Error error)
    {
        Value = default;
        Error = error;
    }

    /// <summary>
    /// Returns a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A successful <see cref="ValueResult{T}"/>.</returns>
    public static ValueResult<T> Success(
        T value) => new(value);

    /// <summary>
    /// Returns a failed result.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed <see cref="ValueResult{T}"/>.</returns>
    public static ValueResult<T> Failure(
        Error error) => new(error);

    /// <summary>
    /// Returns the value, or throws <see cref="InvalidOperationException"/> if this result is a failure.
    /// Prefer explicit <see cref="IsSuccess"/> checks in production code; use this in tests or
    /// when failure genuinely represents a programming error.
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

    /// <summary>Entry point into the async extension method chain.</summary>
    public ValueTask<ValueResult<T>> AsValueTask() => ValueTask.FromResult(this);
}