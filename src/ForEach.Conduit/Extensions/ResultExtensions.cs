namespace ForEach.Conduit.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Result"/> and <see cref="Result{T}"/>.
/// </summary>
public static class ResultExtensions
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
        this Result result,
        Func<TOut> onSuccess,
        Func<Error, TOut> onFailure) =>
        result.IsSuccess ? onSuccess() : onFailure(result.Error!.Value);

    /// <summary>
    /// Executes the specified action if the result is a success.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="onSuccess">The action to execute.</param>
    /// <returns>The original result.</returns>
    public static Result Tap(
        this Result result,
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
    public static Result TapFailure(
        this Result result,
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
    public static Result Recover(
        this Result result,
        Action<Error> recovery)
    {
        if (!result.IsSuccess) recovery(result.Error!.Value);
        return Result.Success();
    }

    /// <summary>
    /// Executes the specified recovery function if the result is a failure.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="recovery">The recovery function.</param>
    /// <returns>The original result if successful, or the result of the recovery function.</returns>
    public static Result RecoverWith(
        this Result result,
        Func<Error, Result> recovery) =>
        result.IsSuccess ? result : recovery(result.Error!.Value);

    /// <summary>
    /// Returns the specified <paramref name="other"/> result if this result is a success.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="other">The other result.</param>
    /// <returns>The <paramref name="other"/> result if successful, otherwise this result.</returns>
    public static Result Combine(
        this Result result,
        Result other) =>
        result.IsSuccess ? other : result;

    /// <summary>
    /// Executes the specified action regardless of the result status.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result.</returns>
    public static Result Finally(
        this Result result,
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
    public static Result<TOut> Map<T, TOut>(
        this Result<T> result,
        Func<T, TOut> mapper) =>
        result.IsSuccess
            ? Result<TOut>.Success(mapper(result.Value!))
            : Result<TOut>.Failure(result.Error!.Value);

    /// <summary>
    /// Chains another operation that returns a result.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="binder">The binding function.</param>
    /// <returns>The result of the chained operation.</returns>
    public static Result<TOut> Bind<T, TOut>(
        this Result<T> result,
        Func<T, Result<TOut>> binder) =>
        result.IsSuccess
            ? binder(result.Value!)
            : Result<TOut>.Failure(result.Error!.Value);

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
        this Result<T> result,
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
    public static Result<T> Tap<T>(
        this Result<T> result,
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
    public static Result<T> TapFailure<T>(
        this Result<T> result,
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
    public static Result<T> Finally<T>(
        this Result<T> result,
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
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Error error) =>
        result.IsSuccess && !predicate(result.Value!) ? Result<T>.Failure(error) : result;

    /// <summary>
    /// Executes the specified recovery function if the result is a failure and returns a successful result with the recovered value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="recovery">The recovery function.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Recover<T>(
        this Result<T> result,
        Func<Error, T> recovery) =>
        result.IsSuccess ? result : Result<T>.Success(recovery(result.Error!.Value));

    /// <summary>
    /// Executes the specified recovery function if the result is a failure.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="recovery">The recovery function.</param>
    /// <returns>The original result if successful, or the result of the recovery function.</returns>
    public static Result<T> RecoverWith<T>(
        this Result<T> result,
        Func<Error, Result<T>> recovery) =>
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
    public static Result<TOut> Combine<T, T2, TOut>(
        this Result<T> result,
        Result<T2> other,
        Func<T, T2, TOut> combiner) =>
        result.IsSuccess && other.IsSuccess
            ? Result<TOut>.Success(
                combiner(
                    result.Value!,
                    other.Value!))
            : result.IsSuccess
                ? Result<TOut>.Failure(other.Error!.Value)
                : Result<TOut>.Failure(result.Error!.Value);

    /// <summary>Bind that intentionally crosses to the stack-allocated path.</summary>
    public static ValueResult<TOut> BindValue<T, TOut>(
        this Result<T> result,
        Func<T, ValueResult<TOut>> binder) =>
        result.IsSuccess
            ? binder(result.Value!)
            : ValueResult<TOut>.Failure(result.Error!.Value);

    /// <summary>
    /// Flattens a nested result.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The nested result.</param>
    /// <returns>The inner result.</returns>
    public static Result<T> Flatten<T>(
        this Result<Result<T>> result) =>
        result.IsSuccess ? result.Value! : Result<T>.Failure(result.Error!.Value);
}