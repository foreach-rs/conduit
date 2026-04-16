namespace ForEach.Conduit;

/// <summary>
/// Immutable value-type error. Designed to be lightweight and allocation-free when used inside ValueResult&lt;T&gt;.
///
/// IMPORTANT: As a struct, <c>default(Error)</c> and <c>new Error()</c> produce an instance with
/// null Code/Message. Always construct via the static factory methods or the public constructor.
/// </summary>
public readonly struct Error : IEquatable<Error>
{
    /// <summary>
    /// The error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// The error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The associated exception, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Child errors when this error aggregates multiple validation failures.
    /// Null on single errors; non-null (and non-empty) on aggregate errors.
    /// </summary>
    public IReadOnlyList<Error>? InnerErrors { get; }

    /// <summary>
    /// Returns <c>true</c> when this instance was constructed via a factory method or public constructor.
    /// Returns <c>false</c> for <c>default(Error)</c> / <c>new Error()</c> — which have a null Code.
    /// </summary>
    public bool IsValid => Code is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> struct.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="exception">The associated exception.</param>
    public Error(
        string code,
        string message,
        Exception? exception = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Exception = exception;
        InnerErrors = null;
    }

    private Error(
        string code,
        string message,
        Exception? exception,
        IReadOnlyList<Error>? innerErrors)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Exception = exception;
        InnerErrors = innerErrors;
    }

    /// <summary>
    /// Creates an <see cref="Error"/> from an exception.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <param name="code">An optional error code. If null, the exception type name is used.</param>
    /// <returns>A new <see cref="Error"/> instance.</returns>
    public static Error FromException(
        Exception ex,
        string? code = null) =>
        new(
            code ?? ex.GetType().Name,
            ex.Message,
            ex);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new <see cref="Error"/> instance with code "Validation.Failed".</returns>
    public static Error Validation(
        string message) =>
        new(
            "Validation.Failed",
            message);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new <see cref="Error"/> instance with code "NotFound".</returns>
    public static Error NotFound(
        string message) =>
        new(
            "NotFound",
            message);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new <see cref="Error"/> instance with code "Conflict".</returns>
    public static Error Conflict(
        string message) =>
        new(
            "Conflict",
            message);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new <see cref="Error"/> instance with code "Unauthorized".</returns>
    public static Error Unauthorized(
        string message) =>
        new(
            "Unauthorized",
            message);

    /// <summary>
    /// Creates a circuit-open error — returned when a circuit breaker is tripped and the
    /// request is rejected without reaching the handler.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new <see cref="Error"/> instance with code "CircuitOpen".</returns>
    public static Error CircuitOpen(
        string message) =>
        new(
            "CircuitOpen",
            message);

    /// <summary>
    /// Creates a timeout error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new <see cref="Error"/> instance with code "Timeout".</returns>
    public static Error Timeout(
        string message) =>
        new(
            "Timeout",
            message);

    /// <summary>
    /// Creates an aggregate error from multiple validation failures.
    /// Use <see cref="InnerErrors"/> to enumerate the individual errors.
    /// </summary>
    /// <param name="errors">The list of errors to aggregate.</param>
    /// <returns>A new aggregate <see cref="Error"/> instance.</returns>
    public static Error Aggregate(
        IReadOnlyList<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
            throw new ArgumentException(
                "Aggregate requires at least one error.",
                nameof(errors));

        return new(
            "Validation.Multiple",
            $"{errors.Count} validation error(s): {string.Join("; ", errors.Select(e => e.Message))}",
            null,
            errors);
    }

    /// <summary>
    /// Convenience overload — collects bare messages into an aggregate validation error.
    /// </summary>
    /// <param name="messages">The error messages.</param>
    /// <returns>A new aggregate <see cref="Error"/> instance.</returns>
    public static Error Aggregate(
        IEnumerable<string> messages) =>
        Aggregate(messages.Select(Validation).ToArray());

    /// <inheritdoc cref="IEquatable{T}" />
    public bool Equals(
        Error other) =>
        Code == other.Code && Message == other.Message;


    /// <inheritdoc cref="IEquatable{T}" />
    public override bool Equals(
        object? obj) =>
        obj is Error other && Equals(other);


    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(
            Code,
            Message);


    /// <inheritdoc cref="IEquatable{T}" />
    public static bool operator ==(
        Error left,
        Error right) => left.Equals(right);


    /// <inheritdoc cref="IEquatable{T}" />
    public static bool operator !=(
        Error left,
        Error right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => $"[{Code}] {Message}";
}