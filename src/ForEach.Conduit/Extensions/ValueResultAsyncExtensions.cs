namespace ForEach.Conduit.Extensions;

/// <summary>
/// Async monads for ValueResult and ValueResult&lt;T&gt;.
///
/// Defined as extension methods on ValueTask&lt;ValueResult&gt; / ValueTask&lt;ValueResult&lt;T&gt;&gt; rather than
/// as instance methods on the struct. This avoids the compiler capturing <c>this</c> as a field in a
/// heap-allocated async state machine, which would box the struct on every async dispatch.
///
/// Usage from a dispatcher result:
/// <code>
///   await dispatcher.Query(q)           // ValueTask&lt;ValueResult&lt;T&gt;&gt;
///       .BindAsync(x => GetData(x))
///       .MapAsync(x => Transform(x))
///       .TapAsync(x => LogAsync(x));
/// </code>
/// </summary>
public static class ValueResultAsyncExtensions
{
    /// <summary>
    /// Executes different asynchronous functions based on whether the result task yields a success or a failure.
    /// </summary>
    /// <typeparam name="TOut">The type of the returned value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="onSuccess">An asynchronous function to execute if the result is a success.</param>
    /// <param name="onFailure">An asynchronous function to execute if the result is a failure.</param>
    /// <returns>A task representing the asynchronous operation, containing the result of the executed function.</returns>
    public static async ValueTask<TOut> MatchAsync<TOut>(
        this ValueTask<ValueResult> resultTask,
        Func<ValueTask<TOut>> onSuccess,
        Func<Error, ValueTask<TOut>> onFailure)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? await onSuccess().ConfigureAwait(false)
            : await onFailure(r.Error!.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an asynchronous action if the result task yields a success and returns the result.
    /// </summary>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="onSuccess">An asynchronous action to execute if the result is a success.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public static async ValueTask<ValueResult> TapAsync(
        this ValueTask<ValueResult> resultTask,
        Func<ValueTask> onSuccess)
    {
        var r = await resultTask.ConfigureAwait(false);
        if (r.IsSuccess) await onSuccess().ConfigureAwait(false);
        return r;
    }

    /// <summary>
    /// Executes an asynchronous action if the result task yields a failure and returns the result.
    /// </summary>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="onFailure">An asynchronous action to execute if the result is a failure.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public static async ValueTask<ValueResult> TapFailureAsync(
        this ValueTask<ValueResult> resultTask,
        Func<Error, ValueTask> onFailure)
    {
        var r = await resultTask.ConfigureAwait(false);
        if (!r.IsSuccess) await onFailure(r.Error!.Value).ConfigureAwait(false);
        return r;
    }

    /// <summary>
    /// Recovers from a failed result task by executing a recovery action and returning a successful result.
    /// </summary>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="recovery">An asynchronous recovery action.</param>
    /// <returns>A task representing the asynchronous operation, containing a successful result.</returns>
    public static async ValueTask<ValueResult> RecoverAsync(
        this ValueTask<ValueResult> resultTask,
        Func<Error, ValueTask> recovery)
    {
        var r = await resultTask.ConfigureAwait(false);
        if (!r.IsSuccess) await recovery(r.Error!.Value).ConfigureAwait(false);
        return ValueResult.Success();
    }

    /// <summary>
    /// Recovers from a failed result task by providing a new result asynchronously.
    /// </summary>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="recovery">An asynchronous function to provide a recovery result.</param>
    /// <returns>A task representing the asynchronous operation, containing the recovered result or the original successful result.</returns>
    public static async ValueTask<ValueResult> RecoverWithAsync(
        this ValueTask<ValueResult> resultTask,
        Func<Error, ValueTask<ValueResult>> recovery)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess ? r : await recovery(r.Error!.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an asynchronous action regardless of whether the result task yields a success or a failure and returns the result.
    /// </summary>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="action">An asynchronous action to execute.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public static async ValueTask<ValueResult> FinallyAsync(
        this ValueTask<ValueResult> resultTask,
        Func<ValueTask> action)
    {
        var r = await resultTask.ConfigureAwait(false);
        await action().ConfigureAwait(false);
        return r;
    }

    /// <summary>
    /// Applies a synchronous <paramref name="mapper"/> to the value of a successful result.
    /// Avoids the allocation overhead of wrapping a sync lambda in Task.FromResult.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <typeparam name="TOut">The type of the mapped result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="mapper">A function to map the value.</param>
    /// <returns>A task representing the asynchronous operation, containing the mapped result.</returns>
    public static async ValueTask<ValueResult<TOut>> Map<T, TOut>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<T, TOut> mapper)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? ValueResult<TOut>.Success(mapper(r.Value!))
            : ValueResult<TOut>.Failure(r.Error!.Value);
    }

    /// <summary>
    /// Chains a synchronous <paramref name="binder"/> that returns a new ValueResult.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <typeparam name="TOut">The type of the bound result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="binder">A function to bind the value to a new result.</param>
    /// <returns>A task representing the asynchronous operation, containing the bound result.</returns>
    public static async ValueTask<ValueResult<TOut>> Bind<T, TOut>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<T, ValueResult<TOut>> binder)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? binder(r.Value!)
            : ValueResult<TOut>.Failure(r.Error!.Value);
    }

    /// <summary>
    /// Chains an asynchronous <paramref name="binder"/> that discards the value and returns a void result.
    /// Useful for "find-then-mutate" pipelines: fetch an entity (typed result),
    /// then perform a side-effectful operation that returns no value.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="binder">An asynchronous function that takes the value and returns a void result.</param>
    /// <returns>A void result task.</returns>
    public static async ValueTask<ValueResult> BindAsync<T>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<T, ValueTask<ValueResult>> binder)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? await binder(r.Value!).ConfigureAwait(false)
            : ValueResult.Failure(r.Error!.Value);
    }

    /// <summary>
    /// Maps the value of a successful result task to a new value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the current result value.</typeparam>
    /// <typeparam name="TOut">The type of the new result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="mapper">An asynchronous function to map the value.</param>
    /// <returns>A task representing the asynchronous operation, containing the mapped result.</returns>
    public static async ValueTask<ValueResult<TOut>> MapAsync<T, TOut>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<T, ValueTask<TOut>> mapper)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? ValueResult<TOut>.Success(await mapper(r.Value!).ConfigureAwait(false))
            : ValueResult<TOut>.Failure(r.Error!.Value);
    }

    /// <summary>
    /// Binds the value of a successful result task to a new result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the current result value.</typeparam>
    /// <typeparam name="TOut">The type of the new result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="binder">An asynchronous function to bind the value to a new result.</param>
    /// <returns>A task representing the asynchronous operation, containing the bound result.</returns>
    public static async ValueTask<ValueResult<TOut>> BindAsync<T, TOut>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<T, ValueTask<ValueResult<TOut>>> binder)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? await binder(r.Value!).ConfigureAwait(false)
            : ValueResult<TOut>.Failure(r.Error!.Value);
    }

    /// <summary>
    /// Executes different asynchronous functions based on whether the result task yields a success or a failure.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <typeparam name="TOut">The type of the returned value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="onSuccess">An asynchronous function to execute if the result is a success.</param>
    /// <param name="onFailure">An asynchronous function to execute if the result is a failure.</param>
    /// <returns>A task representing the asynchronous operation, containing the result of the executed function.</returns>
    public static async ValueTask<TOut> MatchAsync<T, TOut>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<T, ValueTask<TOut>> onSuccess,
        Func<Error, ValueTask<TOut>> onFailure)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? await onSuccess(r.Value!).ConfigureAwait(false)
            : await onFailure(r.Error!.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an asynchronous action if the result task yields a success and returns the result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="onSuccess">An asynchronous action to execute if the result is a success.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public static async ValueTask<ValueResult<T>> TapAsync<T>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<T, ValueTask> onSuccess)
    {
        var r = await resultTask.ConfigureAwait(false);
        if (r.IsSuccess) await onSuccess(r.Value!).ConfigureAwait(false);
        return r;
    }

    /// <summary>
    /// Executes an asynchronous action if the result task yields a failure and returns the result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="onFailure">An asynchronous action to execute if the result is a failure.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public static async ValueTask<ValueResult<T>> TapFailureAsync<T>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<Error, ValueTask> onFailure)
    {
        var r = await resultTask.ConfigureAwait(false);
        if (!r.IsSuccess) await onFailure(r.Error!.Value).ConfigureAwait(false);
        return r;
    }

    /// <summary>
    /// Ensures that a successful result task satisfies a given asynchronous predicate; otherwise, returns a failed result with the specified error.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="predicate">An asynchronous predicate to evaluate.</param>
    /// <param name="error">The error to return if the predicate is not satisfied.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public static async ValueTask<ValueResult<T>> EnsureAsync<T>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<T, ValueTask<bool>> predicate,
        Error error)
    {
        var r = await resultTask.ConfigureAwait(false);
        if (r.IsSuccess && !await predicate(r.Value!).ConfigureAwait(false))
            return ValueResult<T>.Failure(error);
        return r;
    }

    /// <summary>
    /// Recovers from a failed result task by providing a new value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="recovery">An asynchronous function to provide a recovery value.</param>
    /// <returns>A task representing the asynchronous operation, containing a successful result with the recovered value or the original successful result.</returns>
    public static async ValueTask<ValueResult<T>> RecoverAsync<T>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<Error, ValueTask<T>> recovery)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? r
            : ValueResult<T>.Success(await recovery(r.Error!.Value).ConfigureAwait(false));
    }

    /// <summary>
    /// Recovers from a failed result task by providing a new result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="recovery">An asynchronous function to provide a recovery result.</param>
    /// <returns>A task representing the asynchronous operation, containing the recovered result or the original successful result.</returns>
    public static async ValueTask<ValueResult<T>> RecoverWithAsync<T>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<Error, ValueTask<ValueResult<T>>> recovery)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? r
            : await recovery(r.Error!.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an asynchronous action regardless of whether the result task yields a success or a failure and returns the result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="action">An asynchronous action to execute.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public static async ValueTask<ValueResult<T>> FinallyAsync<T>(
        this ValueTask<ValueResult<T>> resultTask,
        Func<ValueTask> action)
    {
        var r = await resultTask.ConfigureAwait(false);
        await action().ConfigureAwait(false);
        return r;
    }

    /// <summary>
    /// Runs both tasks in parallel, then combines their values.
    /// Both tasks run to completion regardless of failure — returns the first failure encountered.
    /// Use when the two operations are independent (e.g. two separate DB queries).
    /// </summary>
    /// <typeparam name="T">The type of the first result value.</typeparam>
    /// <typeparam name="T2">The type of the second result value.</typeparam>
    /// <typeparam name="TOut">The type of the combined result value.</typeparam>
    /// <param name="leftTask">The first result task.</param>
    /// <param name="rightTask">The second result task.</param>
    /// <param name="combiner">A function to combine the two values.</param>
    /// <returns>A task representing the asynchronous operation, containing the combined result.</returns>
    public static async ValueTask<ValueResult<TOut>> CombineAsync<T, T2, TOut>(
        this ValueTask<ValueResult<T>> leftTask,
        ValueTask<ValueResult<T2>> rightTask,
        Func<T, T2, TOut> combiner)
    {
        var left = await leftTask.ConfigureAwait(false);
        var right = await rightTask.ConfigureAwait(false);

        if (!left.IsSuccess) return ValueResult<TOut>.Failure(left.Error!.Value);
        if (!right.IsSuccess) return ValueResult<TOut>.Failure(right.Error!.Value);
        return ValueResult<TOut>.Success(
            combiner(
                left.Value!,
                right.Value!));
    }

    /// <summary>
    /// Runs both tasks in parallel with an async combiner.
    /// Both tasks run to completion regardless of failure — returns the first failure encountered.
    /// </summary>
    /// <typeparam name="T">The type of the first result value.</typeparam>
    /// <typeparam name="T2">The type of the second result value.</typeparam>
    /// <typeparam name="TOut">The type of the combined result value.</typeparam>
    /// <param name="leftTask">The first result task.</param>
    /// <param name="rightTask">The second result task.</param>
    /// <param name="combiner">An asynchronous function to combine the two values.</param>
    /// <returns>A task representing the asynchronous operation, containing the combined result.</returns>
    public static async ValueTask<ValueResult<TOut>> CombineAsync<T, T2, TOut>(
        this ValueTask<ValueResult<T>> leftTask,
        ValueTask<ValueResult<T2>> rightTask,
        Func<T, T2, ValueTask<TOut>> combiner)
    {
        var left = await leftTask.ConfigureAwait(false);
        var right = await rightTask.ConfigureAwait(false);

        if (!left.IsSuccess) return ValueResult<TOut>.Failure(left.Error!.Value);
        if (!right.IsSuccess) return ValueResult<TOut>.Failure(right.Error!.Value);
        return ValueResult<TOut>.Success(
            await combiner(
                left.Value!,
                right.Value!).ConfigureAwait(false));
    }

    /// <summary>
    /// Short-circuits: only starts the right task if the left succeeds.
    /// Use when the right operation depends on the left completing successfully.
    /// </summary>
    /// <typeparam name="T">The type of the first result value.</typeparam>
    /// <typeparam name="T2">The type of the second result value.</typeparam>
    /// <typeparam name="TOut">The type of the combined result value.</typeparam>
    /// <param name="leftTask">The first result task.</param>
    /// <param name="rightFactory">A factory function to create the second result task.</param>
    /// <param name="combiner">A function to combine the two values.</param>
    /// <returns>A task representing the asynchronous operation, containing the combined result.</returns>
    public static async ValueTask<ValueResult<TOut>> CombineSequentialAsync<T, T2, TOut>(
        this ValueTask<ValueResult<T>> leftTask,
        Func<ValueTask<ValueResult<T2>>> rightFactory,
        Func<T, T2, TOut> combiner)
    {
        var left = await leftTask.ConfigureAwait(false);
        if (!left.IsSuccess) return ValueResult<TOut>.Failure(left.Error!.Value);

        var right = await rightFactory().ConfigureAwait(false);
        if (!right.IsSuccess) return ValueResult<TOut>.Failure(right.Error!.Value);

        return ValueResult<TOut>.Success(
            combiner(
                left.Value!,
                right.Value!));
    }
}