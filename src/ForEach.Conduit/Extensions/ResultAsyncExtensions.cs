namespace ForEach.Conduit.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Result{T}"/> to support asynchronous operations.
/// </summary>
public static class ResultAsyncExtensions
{
    /// <summary>
    /// Maps the value of a successful result to a new value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the current result value.</typeparam>
    /// <typeparam name="TOut">The type of the new result value.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="mapper">An asynchronous function to map the value.</param>
    /// <returns>A task representing the asynchronous operation, containing the mapped result.</returns>
    public static async Task<Result<TOut>> MapAsync<T, TOut>(
        this Result<T> result,
        Func<T, Task<TOut>> mapper) =>
        result.IsSuccess
            ? Result<TOut>.Success(await mapper(result.Value!).ConfigureAwait(false))
            : Result<TOut>.Failure(result.Error!.Value);

    /// <summary>
    /// Binds the value of a successful result to a new result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the current result value.</typeparam>
    /// <typeparam name="TOut">The type of the new result value.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="binder">An asynchronous function to bind the value to a new result.</param>
    /// <returns>A task representing the asynchronous operation, containing the bound result.</returns>
    public static async Task<Result<TOut>> BindAsync<T, TOut>(
        this Result<T> result,
        Func<T, Task<Result<TOut>>> binder) =>
        result.IsSuccess
            ? await binder(result.Value!).ConfigureAwait(false)
            : Result<TOut>.Failure(result.Error!.Value);

    /// <summary>
    /// Executes different asynchronous functions based on whether the result is a success or a failure.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <typeparam name="TOut">The type of the returned value.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="onSuccess">An asynchronous function to execute if the result is a success.</param>
    /// <param name="onFailure">An asynchronous function to execute if the result is a failure.</param>
    /// <returns>A task representing the asynchronous operation, containing the result of the executed function.</returns>
    public static async Task<TOut> MatchAsync<T, TOut>(
        this Result<T> result,
        Func<T, Task<TOut>> onSuccess,
        Func<Error, Task<TOut>> onFailure) =>
        result.IsSuccess
            ? await onSuccess(result.Value!).ConfigureAwait(false)
            : await onFailure(result.Error!.Value).ConfigureAwait(false);

    /// <summary>
    /// Executes an asynchronous action if the result is a success and returns the original result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="onSuccess">An asynchronous action to execute if the result is a success.</param>
    /// <returns>A task representing the asynchronous operation, containing the original result.</returns>
    public static async Task<Result<T>> TapAsync<T>(
        this Result<T> result,
        Func<T, Task> onSuccess)
    {
        if (result.IsSuccess) await onSuccess(result.Value!).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Executes an asynchronous action if the result is a failure and returns the original result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="onFailure">An asynchronous action to execute if the result is a failure.</param>
    /// <returns>A task representing the asynchronous operation, containing the original result.</returns>
    public static async Task<Result<T>> TapFailureAsync<T>(
        this Result<T> result,
        Func<Error, Task> onFailure)
    {
        if (!result.IsSuccess) await onFailure(result.Error!.Value).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Ensures that a successful result satisfies a given asynchronous predicate; otherwise, returns a failed result with the specified error.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="predicate">An asynchronous predicate to evaluate.</param>
    /// <param name="error">The error to return if the predicate is not satisfied.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public static async Task<Result<T>> EnsureAsync<T>(
        this Result<T> result,
        Func<T, Task<bool>> predicate,
        Error error) =>
        result.IsSuccess && !await predicate(result.Value!).ConfigureAwait(false)
            ? Result<T>.Failure(error)
            : result;

    /// <summary>
    /// Recovers from a failed result by providing a new value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="recovery">An asynchronous function to provide a recovery value.</param>
    /// <returns>A task representing the asynchronous operation, containing a successful result with the recovered value or the original successful result.</returns>
    public static async Task<Result<T>> RecoverAsync<T>(
        this Result<T> result,
        Func<Error, Task<T>> recovery) =>
        result.IsSuccess
            ? result
            : Result<T>.Success(await recovery(result.Error!.Value).ConfigureAwait(false));

    /// <summary>
    /// Recovers from a failed result by providing a new result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="recovery">An asynchronous function to provide a recovery result.</param>
    /// <returns>A task representing the asynchronous operation, containing the recovered result or the original successful result.</returns>
    public static async Task<Result<T>> RecoverWithAsync<T>(
        this Result<T> result,
        Func<Error, Task<Result<T>>> recovery) =>
        result.IsSuccess
            ? result
            : await recovery(result.Error!.Value).ConfigureAwait(false);

    /// <summary>
    /// Executes an asynchronous action regardless of whether the result is a success or a failure and returns the original result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="action">An asynchronous action to execute.</param>
    /// <returns>A task representing the asynchronous operation, containing the original result.</returns>
    public static async Task<Result<T>> FinallyAsync<T>(
        this Result<T> result,
        Func<Task> action)
    {
        await action().ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Maps the value of a successful result task to a new value.
    /// </summary>
    /// <typeparam name="T">The type of the current result value.</typeparam>
    /// <typeparam name="TOut">The type of the new result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="mapper">A function to map the value.</param>
    /// <returns>A task representing the asynchronous operation, containing the mapped result.</returns>
    public static async Task<Result<TOut>> Map<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, TOut> mapper)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? Result<TOut>.Success(mapper(r.Value!))
            : Result<TOut>.Failure(r.Error!.Value);
    }

    /// <summary>
    /// Binds the value of a successful result task to a new result.
    /// </summary>
    /// <typeparam name="T">The type of the current result value.</typeparam>
    /// <typeparam name="TOut">The type of the new result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="binder">A function to bind the value to a new result.</param>
    /// <returns>A task representing the asynchronous operation, containing the bound result.</returns>
    public static async Task<Result<TOut>> Bind<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, Result<TOut>> binder)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? binder(r.Value!)
            : Result<TOut>.Failure(r.Error!.Value);
    }

    /// <summary>
    /// Maps the value of a successful result task to a new value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the current result value.</typeparam>
    /// <typeparam name="TOut">The type of the new result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="mapper">An asynchronous function to map the value.</param>
    /// <returns>A task representing the asynchronous operation, containing the mapped result.</returns>
    public static async Task<Result<TOut>> MapAsync<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, Task<TOut>> mapper)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? Result<TOut>.Success(await mapper(r.Value!).ConfigureAwait(false))
            : Result<TOut>.Failure(r.Error!.Value);
    }

    /// <summary>
    /// Binds the value of a successful result task to a new result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the current result value.</typeparam>
    /// <typeparam name="TOut">The type of the new result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="binder">An asynchronous function to bind the value to a new result.</param>
    /// <returns>A task representing the asynchronous operation, containing the bound result.</returns>
    public static async Task<Result<TOut>> BindAsync<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result<TOut>>> binder)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? await binder(r.Value!).ConfigureAwait(false)
            : Result<TOut>.Failure(r.Error!.Value);
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
    public static async Task<TOut> MatchAsync<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, Task<TOut>> onSuccess,
        Func<Error, Task<TOut>> onFailure)
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
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task> onSuccess)
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
    public static async Task<Result<T>> TapFailureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, Task> onFailure)
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
    public static async Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<bool>> predicate,
        Error error)
    {
        var r = await resultTask.ConfigureAwait(false);
        if (r.IsSuccess && !await predicate(r.Value!).ConfigureAwait(false))
            return Result<T>.Failure(error);
        return r;
    }

    /// <summary>
    /// Recovers from a failed result task by providing a new value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="recovery">An asynchronous function to provide a recovery value.</param>
    /// <returns>A task representing the asynchronous operation, containing a successful result with the recovered value or the original successful result.</returns>
    public static async Task<Result<T>> RecoverAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, Task<T>> recovery)
    {
        var r = await resultTask.ConfigureAwait(false);
        return r.IsSuccess
            ? r
            : Result<T>.Success(await recovery(r.Error!.Value).ConfigureAwait(false));
    }

    /// <summary>
    /// Recovers from a failed result task by providing a new result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="resultTask">The current result task.</param>
    /// <param name="recovery">An asynchronous function to provide a recovery result.</param>
    /// <returns>A task representing the asynchronous operation, containing the recovered result or the original successful result.</returns>
    public static async Task<Result<T>> RecoverWithAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, Task<Result<T>>> recovery)
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
    public static async Task<Result<T>> FinallyAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Task> action)
    {
        var r = await resultTask.ConfigureAwait(false);
        await action().ConfigureAwait(false);
        return r;
    }

    /// <summary>
    /// Combines two result tasks into a new result asynchronously using a combiner function.
    /// Both tasks are executed in parallel.
    /// </summary>
    /// <typeparam name="T">The type of the first result value.</typeparam>
    /// <typeparam name="T2">The type of the second result value.</typeparam>
    /// <typeparam name="TOut">The type of the combined result value.</typeparam>
    /// <param name="leftTask">The first result task.</param>
    /// <param name="rightTask">The second result task.</param>
    /// <param name="combiner">A function to combine the two values.</param>
    /// <returns>A task representing the asynchronous operation, containing the combined result.</returns>
    public static async Task<Result<TOut>> CombineAsync<T, T2, TOut>(
        this Task<Result<T>> leftTask,
        Task<Result<T2>> rightTask,
        Func<T, T2, TOut> combiner)
    {
        await Task.WhenAll(
            leftTask,
            rightTask).ConfigureAwait(false);
        var left = leftTask.Result;
        var right = rightTask.Result;
        if (!left.IsSuccess) return Result<TOut>.Failure(left.Error!.Value);
        if (!right.IsSuccess) return Result<TOut>.Failure(right.Error!.Value);
        return Result<TOut>.Success(
            combiner(
                left.Value!,
                right.Value!));
    }

    /// <summary>
    /// Combines two result tasks into a new result asynchronously using an asynchronous combiner function.
    /// Both tasks are executed in parallel.
    /// </summary>
    /// <typeparam name="T">The type of the first result value.</typeparam>
    /// <typeparam name="T2">The type of the second result value.</typeparam>
    /// <typeparam name="TOut">The type of the combined result value.</typeparam>
    /// <param name="leftTask">The first result task.</param>
    /// <param name="rightTask">The second result task.</param>
    /// <param name="combiner">An asynchronous function to combine the two values.</param>
    /// <returns>A task representing the asynchronous operation, containing the combined result.</returns>
    public static async Task<Result<TOut>> CombineAsync<T, T2, TOut>(
        this Task<Result<T>> leftTask,
        Task<Result<T2>> rightTask,
        Func<T, T2, Task<TOut>> combiner)
    {
        await Task.WhenAll(
            leftTask,
            rightTask).ConfigureAwait(false);
        var left = leftTask.Result;
        var right = rightTask.Result;
        if (!left.IsSuccess) return Result<TOut>.Failure(left.Error!.Value);
        if (!right.IsSuccess) return Result<TOut>.Failure(right.Error!.Value);
        return Result<TOut>.Success(
            await combiner(
                left.Value!,
                right.Value!).ConfigureAwait(false));
    }

    /// <summary>
    /// Combines two result tasks into a new result sequentially asynchronously.
    /// The second task is only started if the first task succeeds.
    /// </summary>
    /// <typeparam name="T">The type of the first result value.</typeparam>
    /// <typeparam name="T2">The type of the second result value.</typeparam>
    /// <typeparam name="TOut">The type of the combined result value.</typeparam>
    /// <param name="leftTask">The first result task.</param>
    /// <param name="rightFactory">A factory function to create the second result task.</param>
    /// <param name="combiner">A function to combine the two values.</param>
    /// <returns>A task representing the asynchronous operation, containing the combined result.</returns>
    public static async Task<Result<TOut>> CombineSequentialAsync<T, T2, TOut>(
        this Task<Result<T>> leftTask,
        Func<Task<Result<T2>>> rightFactory,
        Func<T, T2, TOut> combiner)
    {
        var left = await leftTask.ConfigureAwait(false);
        if (!left.IsSuccess) return Result<TOut>.Failure(left.Error!.Value);
        var right = await rightFactory().ConfigureAwait(false);
        if (!right.IsSuccess) return Result<TOut>.Failure(right.Error!.Value);
        return Result<TOut>.Success(
            combiner(
                left.Value!,
                right.Value!));
    }
}