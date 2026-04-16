using ForEach.Conduit.Extensions;

namespace ForEach.Conduit.Tests;

public class ResultExtensionsTests
{
    private static readonly Error TestError = Error.NotFound("item");

    [Fact]
    public void Match_OnSuccess_CallsOnSuccess()
    {
        var result = Result.Success().Match(() => "ok", _ => "fail");
        result.Should().Be("ok");
    }

    [Fact]
    public void Match_OnFailure_CallsOnFailure()
    {
        var result = Result.Failure(TestError).Match(() => "ok", e => e.Code);
        result.Should().Be("NotFound");
    }

    [Fact]
    public void Tap_OnSuccess_CallsAction()
    {
        var called = false;
        Result.Success().Tap(() => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void Tap_OnFailure_DoesNotCallAction()
    {
        var called = false;
        Result.Failure(TestError).Tap(() => called = true);
        called.Should().BeFalse();
    }

    [Fact]
    public void Tap_ReturnsOriginalResult()
    {
        var r = Result.Success();
        r.Tap(() => { }).Should().BeSameAs(r);
    }

    [Fact]
    public void TapFailure_OnFailure_CallsAction()
    {
        Error? captured = null;
        Result.Failure(TestError).TapFailure(e => captured = e);
        captured.Should().Be(TestError);
    }

    [Fact]
    public void TapFailure_OnSuccess_DoesNotCallAction()
    {
        var called = false;
        Result.Success().TapFailure(_ => called = true);
        called.Should().BeFalse();
    }

    [Fact]
    public void Recover_OnFailure_CallsRecoveryAndReturnsSuccess()
    {
        var called = false;
        var r = Result.Failure(TestError).Recover(_ => called = true);
        called.Should().BeTrue();
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Recover_OnSuccess_ReturnsSuccess()
    {
        var r = Result.Success().Recover(_ => { });
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RecoverWith_OnFailure_ReturnsRecoveredResult()
    {
        var r = Result.Failure(TestError).RecoverWith(_ => Result.Success());
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RecoverWith_OnSuccess_ReturnsOriginal()
    {
        var success = Result.Success();
        success.RecoverWith(_ => Result.Failure(TestError)).Should().BeSameAs(success);
    }

    [Fact]
    public void Combine_OnSuccess_ReturnsOther()
    {
        var other = Result.Success();
        Result.Success().Combine(other).Should().BeSameAs(other);
    }

    [Fact]
    public void Combine_OnFailure_ReturnsSelf()
    {
        var failure = Result.Failure(TestError);
        failure.Combine(Result.Success()).Should().BeSameAs(failure);
    }

    [Fact]
    public void Finally_AlwaysCallsAction_OnSuccess()
    {
        var called = false;
        Result.Success().Finally(() => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void Finally_AlwaysCallsAction_OnFailure()
    {
        var called = false;
        Result.Failure(TestError).Finally(() => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        var r = Result<int>.Success(5).Map(x => x * 2);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(10);
    }

    [Fact]
    public void Map_OnFailure_PropagatesError()
    {
        var r = Result<int>.Failure(TestError).Map(x => x * 2);
        r.IsSuccess.Should().BeFalse();
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public void Bind_OnSuccess_ChainsOperation()
    {
        var r = Result<int>.Success(3).Bind(x => Result<string>.Success($"val:{x}"));
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be("val:3");
    }

    [Fact]
    public void Bind_OnFailure_ShortCircuits()
    {
        var called = false;
        Result<int>.Failure(TestError).Bind(x => { called = true; return Result<string>.Success("x"); });
        called.Should().BeFalse();
    }

    [Fact]
    public void GenericMatch_OnSuccess_CallsOnSuccess()
    {
        var r = Result<int>.Success(7).Match(v => $"v={v}", _ => "fail");
        r.Should().Be("v=7");
    }

    [Fact]
    public void GenericMatch_OnFailure_CallsOnFailure()
    {
        var r = Result<int>.Failure(TestError).Match(v => "ok", e => e.Code);
        r.Should().Be("NotFound");
    }

    [Fact]
    public void GenericTap_OnSuccess_CallsAction()
    {
        int? captured = null;
        Result<int>.Success(42).Tap(v => captured = v);
        captured.Should().Be(42);
    }

    [Fact]
    public void GenericTap_OnFailure_DoesNotCallAction()
    {
        var called = false;
        Result<int>.Failure(TestError).Tap(_ => called = true);
        called.Should().BeFalse();
    }

    [Fact]
    public void GenericTapFailure_OnFailure_CallsAction()
    {
        Error? captured = null;
        Result<int>.Failure(TestError).TapFailure(e => captured = e);
        captured.Should().Be(TestError);
    }

    [Fact]
    public void GenericTapFailure_OnSuccess_DoesNotCallAction()
    {
        var called = false;
        Result<int>.Success(1).TapFailure(_ => called = true);
        called.Should().BeFalse();
    }

    [Fact]
    public void GenericFinally_AlwaysCalled_OnSuccess()
    {
        var called = false;
        Result<int>.Success(1).Finally(() => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void GenericFinally_AlwaysCalled_OnFailure()
    {
        var called = false;
        Result<int>.Failure(TestError).Finally(() => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void Ensure_OnSuccess_PredicateMet_PassesThrough()
    {
        var r = Result<int>.Success(10).Ensure(x => x > 0, TestError);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(10);
    }

    [Fact]
    public void Ensure_OnSuccess_PredicateNotMet_ReturnsFailure()
    {
        var r = Result<int>.Success(-1).Ensure(x => x > 0, TestError);
        r.IsSuccess.Should().BeFalse();
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public void Ensure_OnFailure_PredicateNotInvoked()
    {
        var called = false;
        Result<int>.Failure(TestError).Ensure(x => { called = true; return true; }, TestError);
        called.Should().BeFalse();
    }

    [Fact]
    public void GenericRecover_OnFailure_ReturnsSuccessWithRecoveredValue()
    {
        var r = Result<int>.Failure(TestError).Recover(_ => -1);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(-1);
    }

    [Fact]
    public void GenericRecover_OnSuccess_ReturnsOriginal()
    {
        var r = Result<int>.Success(5).Recover(_ => -1);
        r.Value.Should().Be(5);
    }

    [Fact]
    public void GenericRecoverWith_OnFailure_CallsRecovery()
    {
        var r = Result<int>.Failure(TestError).RecoverWith(_ => Result<int>.Success(99));
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(99);
    }

    [Fact]
    public void GenericRecoverWith_OnSuccess_ReturnsOriginal()
    {
        var r = Result<int>.Success(5).RecoverWith(_ => Result<int>.Success(99));
        r.Value.Should().Be(5);
    }

    [Fact]
    public void GenericCombine_BothSuccess_ReturnsCombinedValue()
    {
        var r = Result<int>.Success(3).Combine(Result<int>.Success(4), (a, b) => a + b);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(7);
    }

    [Fact]
    public void GenericCombine_FirstFailure_ReturnsFirstError()
    {
        var err = Error.Validation("a");
        var r = Result<int>.Failure(err).Combine(Result<int>.Success(4), (a, b) => a + b);
        r.Error.Should().Be(err);
    }

    [Fact]
    public void GenericCombine_SecondFailure_ReturnsSecondError()
    {
        var err = Error.Validation("b");
        var r = Result<int>.Success(3).Combine(Result<int>.Failure(err), (a, b) => a + b);
        r.Error.Should().Be(err);
    }

    [Fact]
    public void BindValue_OnSuccess_CrossesToValueResult()
    {
        var r = Result<int>.Success(5).BindValue(x => ValueResult<string>.Success($"v={x}"));
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be("v=5");
    }

    [Fact]
    public void BindValue_OnFailure_PropagatesError()
    {
        var r = Result<int>.Failure(TestError).BindValue(x => ValueResult<string>.Success("ok"));
        r.IsSuccess.Should().BeFalse();
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public void Flatten_InnerSuccess_ReturnsInner()
    {
        var inner = Result<int>.Success(7);
        var r = Result<Result<int>>.Success(inner).Flatten();
        r.Should().BeSameAs(inner);
    }

    [Fact]
    public void Flatten_OuterFailure_PropagatesError()
    {
        var r = Result<Result<int>>.Failure(TestError).Flatten();
        r.Error.Should().Be(TestError);
    }
}
