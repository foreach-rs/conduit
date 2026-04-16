namespace ForEach.Conduit.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ValueResult"/> and <see cref="ValueResult{T}"/>.
/// </summary>
public static class ValueResultExtensions
{
    /// <summary>
    /// Executes <paramref name="onSuccess"/> if the result is a success, or <paramref name="onFailure"/> if it is a failure.
    /// </summary>
    /// <typeparam name="TOut">The type of the output.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="onSuccess">The function to execute on success.</param>
    /// <param name="onFailure">The function to execute on failure.</param>
    /// <returns>The result of the executed function.</returns>
    public static TOut Match<TOut>(
        this ValueResult result,
        Func<TOut> onSuccess,
        Func<Error, TOut> onFailure) =>
        result.IsSuccess ? onSuccess() : onFailure(result.Error!.Value);

    /// <summary>
    /// Executes the specified action if the result is a success.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="onSuccess">The action to execute.</param>
    /// <returns>The original result.</returns>
    public static ValueResult Tap(
        this ValueResult result,
        Action onSuccess)
    {
        if (result.IsSuccess) onSuccess();
        return result;
    }

    /// <summary>
    /// Executes the specified action if the result is a failure.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="onFailure">The action to execute with the error.</param>
    /// <returns>The original result.</returns>
    public static ValueResult TapFailure(
        this ValueResult result,
        Action<Error> onFailure)
    {
        if (!result.IsSuccess) onFailure(result.Error!.Value);
        return result;
    }

    /// <summary>
    /// Executes the specified recovery action if the result is a failure and returns a successful result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="recovery">The recovery action.</param>
    /// <returns>A successful result.</returns>
    public static ValueResult Recover(
        this ValueResult result,
        Action<Error> recovery)
    {
        if (!result.IsSuccess) recovery(result.Error!.Value);
        return ValueResult.Success();
    }

    /// <summary>
    /// Executes the specified recovery function if the result is a failure.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="recovery">The recovery function.</param>
    /// <returns>The original result if successful, or the result of the recovery function.</returns>
    public static ValueResult RecoverWith(
        this ValueResult result,
        Func<Error, ValueResult> recovery) =>
        result.IsSuccess ? result : recovery(result.Error!.Value);

    /// <summary>
    /// Returns the specified <paramref name="other"/> result if this result is a success.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="other">The other result.</param>
    /// <returns>The <paramref name="other"/> result if successful, otherwise this result.</returns>
    public static ValueResult Combine(
        this ValueResult result,
        ValueResult other) =>
        result.IsSuccess ? other : result;

    /// <summary>
    /// Executes the specified action regardless of the result status.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result.</returns>
    public static ValueResult Finally(
        this ValueResult result,
        Action action)
    {
        action();
        return result;
    }

    /// <summary>
    /// Transforms the value of a successful result.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new result with the transformed value.</returns>
    public static ValueResult<TOut> Map<T, TOut>(
        this ValueResult<T> result,
        Func<T, TOut> mapper) =>
        result.IsSuccess
            ? ValueResult<TOut>.Success(mapper(result.Value!))
            : ValueResult<TOut>.Failure(result.Error!.Value);

    /// <summary>
    /// Chains another operation that returns a result.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="binder">The binding function.</param>
    /// <returns>The result of the chained operation.</returns>
    public static ValueResult<TOut> Bind<T, TOut>(
        this ValueResult<T> result,
        Func<T, ValueResult<TOut>> binder) =>
        result.IsSuccess
            ? binder(result.Value!)
            : ValueResult<TOut>.Failure(result.Error!.Value);

    /// <summary>
    /// Executes <paramref name="onSuccess"/> if the result is a success, or <paramref name="onFailure"/> if it is a failure.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <typeparam name="TOut">The type of the output.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="onSuccess">The function to execute on success.</param>
    /// <param name="onFailure">The function to execute on failure.</param>
    /// <returns>The result of the executed function.</returns>
    public static TOut Match<T, TOut>(
        this ValueResult<T> result,
        Func<T, TOut> onSuccess,
        Func<Error, TOut> onFailure) =>
        result.IsSuccess ? onSuccess(result.Value!) : onFailure(result.Error!.Value);

    /// <summary>
    /// Executes the specified action if the result is a success.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="onSuccess">The action to execute.</param>
    /// <returns>The original result.</returns>
    public static ValueResult<T> Tap<T>(
        this ValueResult<T> result,
        Action<T> onSuccess)
    {
        if (result.IsSuccess) onSuccess(result.Value!);
        return result;
    }

    /// <summary>
    /// Executes the specified action if the result is a failure.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="onFailure">The action to execute with the error.</param>
    /// <returns>The original result.</returns>
    public static ValueResult<T> TapFailure<T>(
        this ValueResult<T> result,
        Action<Error> onFailure)
    {
        if (!result.IsSuccess) onFailure(result.Error!.Value);
        return result;
    }

    /// <summary>
    /// Executes the specified action regardless of the result status.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result.</returns>
    public static ValueResult<T> Finally<T>(
        this ValueResult<T> result,
        Action action)
    {
        action();
        return result;
    }

    /// <summary>
    /// Returns a failed result if the predicate is not met.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="predicate">The condition to meet.</param>
    /// <param name="error">The error to return if the predicate is not met.</param>
    /// <returns>The original result if the predicate is met, otherwise a failed result.</returns>
    public static ValueResult<T> Ensure<T>(
        this ValueResult<T> result,
        Func<T, bool> predicate,
        Error error) =>
        result.IsSuccess && !predicate(result.Value!) ? ValueResult<T>.Failure(error) : result;

    /// <summary>
    /// Executes the specified recovery function if the result is a failure and returns a successful result with the recovered value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="recovery">The recovery function.</param>
    /// <returns>A successful result.</returns>
    public static ValueResult<T> Recover<T>(
        this ValueResult<T> result,
        Func<Error, T> recovery) =>
        result.IsSuccess ? result : ValueResult<T>.Success(recovery(result.Error!.Value));

    /// <summary>
    /// Executes the specified recovery function if the result is a failure.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="recovery">The recovery function.</param>
    /// <returns>The original result if successful, or the result of the recovery function.</returns>
    public static ValueResult<T> RecoverWith<T>(
        this ValueResult<T> result,
        Func<Error, ValueResult<T>> recovery) =>
        result.IsSuccess ? result : recovery(result.Error!.Value);

    /// <summary>
    /// Combines two results using the specified combiner function.
    /// </summary>
    /// <typeparam name="T">The type of the first value.</typeparam>
    /// <typeparam name="T2">The type of the second value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The first result.</param>
    /// <param name="other">The second result.</param>
    /// <param name="combiner">The combiner function.</param>
    /// <returns>A successful result with the combined value if both results are successful, otherwise a failed result.</returns>
    public static ValueResult<TOut> Combine<T, T2, TOut>(
        this ValueResult<T> result,
        ValueResult<T2> other,
        Func<T, T2, TOut> combiner) =>
        result.IsSuccess && other.IsSuccess
            ? ValueResult<TOut>.Success(
                combiner(
                    result.Value!,
                    other.Value!))
            : result.IsSuccess
                ? ValueResult<TOut>.Failure(other.Error!.Value)
                : ValueResult<TOut>.Failure(result.Error!.Value);

    /// <summary>Bind that intentionally crosses to the heap-allocated path.</summary>
    public static Result<TOut> BindHeap<T, TOut>(
        this ValueResult<T> result,
        Func<T, Result<TOut>> binder) =>
        result.IsSuccess
            ? binder(result.Value!)
            : Result<TOut>.Failure(result.Error!.Value);

    /// <summary>
    /// Flattens a nested result.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The nested result.</param>
    /// <returns>The inner result.</returns>
    public static ValueResult<T> Flatten<T>(
        this ValueResult<ValueResult<T>> result) =>
        result.IsSuccess ? result.Value : ValueResult<T>.Failure(result.Error!.Value);
}