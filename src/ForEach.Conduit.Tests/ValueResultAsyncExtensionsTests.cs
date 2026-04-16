using ForEach.Conduit.Extensions;

namespace ForEach.Conduit.Tests;

public class ValueResultAsyncExtensionsTests
{
    private static readonly Error TestError = Error.Unauthorized("no access");

    [Fact]
    public async Task MatchAsync_Void_OnSuccess_CallsOnSuccess()
    {
        var r = await ValueResult.Success().AsValueTask()
            .MatchAsync(() => ValueTask.FromResult("ok"), _ => ValueTask.FromResult("fail"));
        r.Should().Be("ok");
    }

    [Fact]
    public async Task MatchAsync_Void_OnFailure_CallsOnFailure()
    {
        var r = await ValueResult.Failure(TestError).AsValueTask()
            .MatchAsync(() => ValueTask.FromResult("ok"), e => ValueTask.FromResult(e.Code));
        r.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task TapAsync_Void_OnSuccess_CallsAction()
    {
        var called = false;
        await ValueResult.Success().AsValueTask()
            .TapAsync(() => { called = true; return ValueTask.CompletedTask; });
        called.Should().BeTrue();
    }

    [Fact]
    public async Task TapAsync_Void_OnFailure_DoesNotCallAction()
    {
        var called = false;
        await ValueResult.Failure(TestError).AsValueTask()
            .TapAsync(() => { called = true; return ValueTask.CompletedTask; });
        called.Should().BeFalse();
    }

    [Fact]
    public async Task TapFailureAsync_Void_OnFailure_CallsAction()
    {
        Error? captured = null;
        await ValueResult.Failure(TestError).AsValueTask()
            .TapFailureAsync(e => { captured = e; return ValueTask.CompletedTask; });
        captured.Should().Be(TestError);
    }

    [Fact]
    public async Task RecoverAsync_Void_OnFailure_ReturnsSuccess()
    {
        var r = await ValueResult.Failure(TestError).AsValueTask()
            .RecoverAsync(_ => ValueTask.CompletedTask);
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverWithAsync_Void_OnFailure_ReturnsRecovered()
    {
        var r = await ValueResult.Failure(TestError).AsValueTask()
            .RecoverWithAsync(_ => ValueTask.FromResult(ValueResult.Success()));
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task FinallyAsync_Void_AlwaysCalled_OnSuccess()
    {
        var called = false;
        await ValueResult.Success().AsValueTask()
            .FinallyAsync(() => { called = true; return ValueTask.CompletedTask; });
        called.Should().BeTrue();
    }

    [Fact]
    public async Task FinallyAsync_Void_AlwaysCalled_OnFailure()
    {
        var called = false;
        await ValueResult.Failure(TestError).AsValueTask()
            .FinallyAsync(() => { called = true; return ValueTask.CompletedTask; });
        called.Should().BeTrue();
    }

    [Fact]
    public async Task Map_OnSuccess_TransformsValue()
    {
        var r = await ValueResult<int>.Success(5).AsValueTask()
            .Map(x => x * 4);
        r.Value.Should().Be(20);
    }

    [Fact]
    public async Task Map_OnFailure_PropagatesError()
    {
        var r = await ValueResult<int>.Failure(TestError).AsValueTask()
            .Map(x => x * 4);
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public async Task Bind_OnSuccess_ChainsOperation()
    {
        var r = await ValueResult<int>.Success(3).AsValueTask()
            .Bind(x => ValueResult<string>.Success($"v={x}"));
        r.Value.Should().Be("v=3");
    }

    [Fact]
    public async Task Bind_OnFailure_ShortCircuits()
    {
        var called = false;
        await ValueResult<int>.Failure(TestError).AsValueTask()
            .Bind(x => { called = true; return ValueResult<string>.Success("x"); });
        called.Should().BeFalse();
    }

    [Fact]
    public async Task BindAsync_Void_OnSuccess_ChainsToVoidResult()
    {
        var r = await ValueResult<int>.Success(1).AsValueTask()
            .BindAsync(_ => ValueTask.FromResult(ValueResult.Success()));
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BindAsync_Void_OnFailure_PropagatesError()
    {
        var r = await ValueResult<int>.Failure(TestError).AsValueTask()
            .BindAsync(_ => ValueTask.FromResult(ValueResult.Success()));
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public async Task MapAsync_OnSuccess_TransformsValue()
    {
        var r = await ValueResult<int>.Success(2).AsValueTask()
            .MapAsync(x => ValueTask.FromResult(x + 8));
        r.Value.Should().Be(10);
    }

    [Fact]
    public async Task BindAsync_Generic_OnSuccess_ChainsOperation()
    {
        var r = await ValueResult<int>.Success(7).AsValueTask()
            .BindAsync(x => ValueTask.FromResult(ValueResult<string>.Success($"n={x}")));
        r.Value.Should().Be("n=7");
    }

    [Fact]
    public async Task MatchAsync_OnSuccess_CallsOnSuccess()
    {
        var r = await ValueResult<int>.Success(11).AsValueTask()
            .MatchAsync(v => ValueTask.FromResult($"v={v}"), _ => ValueTask.FromResult("err"));
        r.Should().Be("v=11");
    }

    [Fact]
    public async Task TapAsync_OnSuccess_CallsAction()
    {
        int? captured = null;
        await ValueResult<int>.Success(99).AsValueTask()
            .TapAsync(v => { captured = v; return ValueTask.CompletedTask; });
        captured.Should().Be(99);
    }

    [Fact]
    public async Task TapFailureAsync_OnFailure_CallsAction()
    {
        Error? captured = null;
        await ValueResult<int>.Failure(TestError).AsValueTask()
            .TapFailureAsync(e => { captured = e; return ValueTask.CompletedTask; });
        captured.Should().Be(TestError);
    }

    [Fact]
    public async Task EnsureAsync_PredicateMet_PassesThrough()
    {
        var r = await ValueResult<int>.Success(5).AsValueTask()
            .EnsureAsync(x => ValueTask.FromResult(x > 0), TestError);
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureAsync_PredicateNotMet_ReturnsFailure()
    {
        var r = await ValueResult<int>.Success(-3).AsValueTask()
            .EnsureAsync(x => ValueTask.FromResult(x > 0), TestError);
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public async Task RecoverAsync_OnFailure_ReturnsSuccessWithRecoveredValue()
    {
        var r = await ValueResult<int>.Failure(TestError).AsValueTask()
            .RecoverAsync(_ => ValueTask.FromResult(-1));
        r.Value.Should().Be(-1);
    }

    [Fact]
    public async Task RecoverWithAsync_OnFailure_CallsRecovery()
    {
        var r = await ValueResult<int>.Failure(TestError).AsValueTask()
            .RecoverWithAsync(_ => ValueTask.FromResult(ValueResult<int>.Success(42)));
        r.Value.Should().Be(42);
    }

    [Fact]
    public async Task FinallyAsync_AlwaysCalled_OnSuccess()
    {
        var called = false;
        await ValueResult<int>.Success(1).AsValueTask()
            .FinallyAsync(() => { called = true; return ValueTask.CompletedTask; });
        called.Should().BeTrue();
    }

    [Fact]
    public async Task CombineAsync_BothSuccess_CombinesValues()
    {
        var r = await ValueResult<int>.Success(3).AsValueTask()
            .CombineAsync(ValueResult<int>.Success(4).AsValueTask(), (a, b) => a + b);
        r.Value.Should().Be(7);
    }

    [Fact]
    public async Task CombineAsync_FirstFailure_ReturnsFirstError()
    {
        var err = Error.Validation("first");
        var r = await ValueResult<int>.Failure(err).AsValueTask()
            .CombineAsync(ValueResult<int>.Success(1).AsValueTask(), (a, b) => a + b);
        r.Error.Should().Be(err);
    }

    [Fact]
    public async Task CombineAsync_AsyncCombiner_CombinesValues()
    {
        var r = await ValueResult<int>.Success(3).AsValueTask()
            .CombineAsync(ValueResult<int>.Success(3).AsValueTask(),
                (a, b) => ValueTask.FromResult(a * b));
        r.Value.Should().Be(9);
    }

    [Fact]
    public async Task CombineSequentialAsync_FirstFailure_DoesNotStartSecond()
    {
        var secondStarted = false;
        var err = Error.Validation("first");
        var r = await ValueResult<int>.Failure(err).AsValueTask()
            .CombineSequentialAsync(() =>
            {
                secondStarted = true;
                return ValueTask.FromResult(ValueResult<int>.Success(1));
            }, (a, b) => a + b);
        secondStarted.Should().BeFalse();
        r.Error.Should().Be(err);
    }

    [Fact]
    public async Task CombineSequentialAsync_BothSuccess_CombinesValues()
    {
        var r = await ValueResult<int>.Success(10).AsValueTask()
            .CombineSequentialAsync(
                () => ValueTask.FromResult(ValueResult<int>.Success(3)),
                (a, b) => a - b);
        r.Value.Should().Be(7);
    }
}
