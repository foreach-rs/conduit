using ForEach.Conduit.Extensions;

namespace ForEach.Conduit.Tests;

public class ResultAsyncExtensionsTests
{
    private static readonly Error TestError = Error.NotFound("entity");

    [Fact]
    public async Task MapAsync_OnSuccess_TransformsValue()
    {
        var r = await Result<int>.Success(3).MapAsync(x => Task.FromResult(x * 10));
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(30);
    }

    [Fact]
    public async Task MapAsync_OnFailure_PropagatesError()
    {
        var r = await Result<int>.Failure(TestError).MapAsync(x => Task.FromResult(x * 10));
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public async Task BindAsync_OnSuccess_ChainsOperation()
    {
        var r = await Result<int>.Success(5)
            .BindAsync(x => Task.FromResult(Result<string>.Success($"n={x}")));
        r.Value.Should().Be("n=5");
    }

    [Fact]
    public async Task BindAsync_OnFailure_ShortCircuits()
    {
        var called = false;
        await Result<int>.Failure(TestError)
            .BindAsync(x => { called = true; return Task.FromResult(Result<string>.Success("x")); });
        called.Should().BeFalse();
    }

    [Fact]
    public async Task MatchAsync_OnSuccess_CallsOnSuccess()
    {
        var r = await Result<int>.Success(7)
            .MatchAsync(v => Task.FromResult($"v={v}"), _ => Task.FromResult("err"));
        r.Should().Be("v=7");
    }

    [Fact]
    public async Task MatchAsync_OnFailure_CallsOnFailure()
    {
        var r = await Result<int>.Failure(TestError)
            .MatchAsync(v => Task.FromResult("ok"), e => Task.FromResult(e.Code));
        r.Should().Be("NotFound");
    }

    [Fact]
    public async Task TapAsync_OnSuccess_CallsAction()
    {
        int? captured = null;
        await Result<int>.Success(42).TapAsync(v => { captured = v; return Task.CompletedTask; });
        captured.Should().Be(42);
    }

    [Fact]
    public async Task TapAsync_OnFailure_DoesNotCallAction()
    {
        var called = false;
        await Result<int>.Failure(TestError).TapAsync(_ => { called = true; return Task.CompletedTask; });
        called.Should().BeFalse();
    }

    [Fact]
    public async Task TapFailureAsync_OnFailure_CallsAction()
    {
        Error? captured = null;
        await Result<int>.Failure(TestError).TapFailureAsync(e => { captured = e; return Task.CompletedTask; });
        captured.Should().Be(TestError);
    }

    [Fact]
    public async Task EnsureAsync_OnSuccess_PredicateMet_PassesThrough()
    {
        var r = await Result<int>.Success(5).EnsureAsync(x => Task.FromResult(x > 0), TestError);
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureAsync_OnSuccess_PredicateNotMet_ReturnsFailure()
    {
        var r = await Result<int>.Success(-1).EnsureAsync(x => Task.FromResult(x > 0), TestError);
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public async Task RecoverAsync_OnFailure_ReturnsSuccessWithRecoveredValue()
    {
        var r = await Result<int>.Failure(TestError).RecoverAsync(_ => Task.FromResult(-1));
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(-1);
    }

    [Fact]
    public async Task RecoverAsync_OnSuccess_ReturnsOriginal()
    {
        var r = await Result<int>.Success(5).RecoverAsync(_ => Task.FromResult(-1));
        r.Value.Should().Be(5);
    }

    [Fact]
    public async Task RecoverWithAsync_OnFailure_CallsRecovery()
    {
        var r = await Result<int>.Failure(TestError)
            .RecoverWithAsync(_ => Task.FromResult(Result<int>.Success(99)));
        r.Value.Should().Be(99);
    }

    [Fact]
    public async Task FinallyAsync_AlwaysCalled_OnSuccess()
    {
        var called = false;
        await Result<int>.Success(1).FinallyAsync(() => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
    }

    [Fact]
    public async Task FinallyAsync_AlwaysCalled_OnFailure()
    {
        var called = false;
        await Result<int>.Failure(TestError).FinallyAsync(() => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
    }

    [Fact]
    public async Task Task_Map_OnSuccess_TransformsValue()
    {
        var r = await Task.FromResult(Result<int>.Success(4)).Map(x => x * 5);
        r.Value.Should().Be(20);
    }

    [Fact]
    public async Task Task_Map_OnFailure_PropagatesError()
    {
        var r = await Task.FromResult(Result<int>.Failure(TestError)).Map(x => x * 5);
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public async Task Task_Bind_OnSuccess_ChainsOperation()
    {
        var r = await Task.FromResult(Result<int>.Success(2))
            .Bind(x => Result<string>.Success($"x={x}"));
        r.Value.Should().Be("x=2");
    }

    [Fact]
    public async Task Task_MapAsync_OnSuccess_TransformsValue()
    {
        var r = await Task.FromResult(Result<int>.Success(3))
            .MapAsync(x => Task.FromResult(x + 1));
        r.Value.Should().Be(4);
    }

    [Fact]
    public async Task Task_BindAsync_OnSuccess_ChainsOperation()
    {
        var r = await Task.FromResult(Result<int>.Success(6))
            .BindAsync(x => Task.FromResult(Result<string>.Success($"ok={x}")));
        r.Value.Should().Be("ok=6");
    }

    [Fact]
    public async Task Task_TapAsync_OnSuccess_CallsAction()
    {
        int? v = null;
        await Task.FromResult(Result<int>.Success(8))
            .TapAsync(x => { v = x; return Task.CompletedTask; });
        v.Should().Be(8);
    }

    [Fact]
    public async Task Task_RecoverAsync_OnFailure_ReturnsRecovered()
    {
        var r = await Task.FromResult(Result<int>.Failure(TestError))
            .RecoverAsync(_ => Task.FromResult(0));
        r.Value.Should().Be(0);
    }

    [Fact]
    public async Task Task_FinallyAsync_AlwaysCalled()
    {
        var called = false;
        await Task.FromResult(Result<int>.Success(1))
            .FinallyAsync(() => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
    }

    [Fact]
    public async Task CombineAsync_BothSuccess_CombinesValues()
    {
        var r = await Task.FromResult(Result<int>.Success(3))
            .CombineAsync(Task.FromResult(Result<int>.Success(4)), (a, b) => a + b);
        r.Value.Should().Be(7);
    }

    [Fact]
    public async Task CombineAsync_FirstFailure_ReturnsFirstError()
    {
        var err = Error.Validation("first");
        var r = await Task.FromResult(Result<int>.Failure(err))
            .CombineAsync(Task.FromResult(Result<int>.Success(1)), (a, b) => a + b);
        r.Error.Should().Be(err);
    }

    [Fact]
    public async Task CombineAsync_SecondFailure_ReturnsSecondError()
    {
        var err = Error.Validation("second");
        var r = await Task.FromResult(Result<int>.Success(1))
            .CombineAsync(Task.FromResult(Result<int>.Failure(err)), (a, b) => a + b);
        r.Error.Should().Be(err);
    }

    [Fact]
    public async Task CombineAsync_AsyncCombiner_CombinesValues()
    {
        var r = await Task.FromResult(Result<int>.Success(2))
            .CombineAsync(Task.FromResult(Result<int>.Success(3)),
                (a, b) => Task.FromResult(a * b));
        r.Value.Should().Be(6);
    }

    [Fact]
    public async Task CombineSequentialAsync_BothSuccess_CombinesValues()
    {
        var r = await Task.FromResult(Result<int>.Success(10))
            .CombineSequentialAsync(() => Task.FromResult(Result<int>.Success(5)), (a, b) => a - b);
        r.Value.Should().Be(5);
    }

    [Fact]
    public async Task CombineSequentialAsync_FirstFailure_DoesNotStartSecond()
    {
        var secondStarted = false;
        var err = Error.Validation("first");
        var r = await Task.FromResult(Result<int>.Failure(err))
            .CombineSequentialAsync(() =>
            {
                secondStarted = true;
                return Task.FromResult(Result<int>.Success(1));
            }, (a, b) => a + b);
        secondStarted.Should().BeFalse();
        r.Error.Should().Be(err);
    }
}
