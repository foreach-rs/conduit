using ForEach.Conduit.Extensions;

namespace ForEach.Conduit.Tests;

public class ValueResultExtensionsTests
{
    private static readonly Error TestError = Error.Validation("bad input");

    [Fact]
    public void Match_OnSuccess_CallsOnSuccess()
    {
        var r = ValueResult.Success().Match(() => "ok", _ => "fail");
        r.Should().Be("ok");
    }

    [Fact]
    public void Match_OnFailure_CallsOnFailure()
    {
        var r = ValueResult.Failure(TestError).Match(() => "ok", e => e.Code);
        r.Should().Be("Validation.Failed");
    }

    [Fact]
    public void Tap_OnSuccess_CallsAction()
    {
        var called = false;
        ValueResult.Success().Tap(() => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void Tap_OnFailure_DoesNotCallAction()
    {
        var called = false;
        ValueResult.Failure(TestError).Tap(() => called = true);
        called.Should().BeFalse();
    }

    [Fact]
    public void TapFailure_OnFailure_CallsAction()
    {
        Error? captured = null;
        ValueResult.Failure(TestError).TapFailure(e => captured = e);
        captured.Should().Be(TestError);
    }

    [Fact]
    public void TapFailure_OnSuccess_DoesNotCallAction()
    {
        var called = false;
        ValueResult.Success().TapFailure(_ => called = true);
        called.Should().BeFalse();
    }

    [Fact]
    public void Recover_OnFailure_CallsRecoveryAndReturnsSuccess()
    {
        var called = false;
        var r = ValueResult.Failure(TestError).Recover(_ => called = true);
        called.Should().BeTrue();
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Recover_OnSuccess_AlsoReturnsSuccess()
    {
        var r = ValueResult.Success().Recover(_ => { });
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RecoverWith_OnFailure_ReturnsRecoveredResult()
    {
        var r = ValueResult.Failure(TestError).RecoverWith(_ => ValueResult.Success());
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RecoverWith_OnSuccess_ReturnsOriginal()
    {
        var success = ValueResult.Success();
        success.RecoverWith(_ => ValueResult.Failure(TestError)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Combine_OnSuccess_ReturnsOther()
    {
        var other = ValueResult.Failure(TestError);
        ValueResult.Success().Combine(other).Should().Be(other);
    }

    [Fact]
    public void Combine_OnFailure_ReturnsSelf()
    {
        var failure = ValueResult.Failure(TestError);
        failure.Combine(ValueResult.Success()).Should().Be(failure);
    }

    [Fact]
    public void Finally_AlwaysCalled_OnSuccess()
    {
        var called = false;
        ValueResult.Success().Finally(() => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void Finally_AlwaysCalled_OnFailure()
    {
        var called = false;
        ValueResult.Failure(TestError).Finally(() => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        var r = ValueResult<int>.Success(5).Map(x => x * 3);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(15);
    }

    [Fact]
    public void Map_OnFailure_PropagatesError()
    {
        var r = ValueResult<int>.Failure(TestError).Map(x => x * 3);
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public void Bind_OnSuccess_ChainsOperation()
    {
        var r = ValueResult<int>.Success(4).Bind(x => ValueResult<string>.Success($"n={x}"));
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be("n=4");
    }

    [Fact]
    public void Bind_OnFailure_ShortCircuits()
    {
        var called = false;
        ValueResult<int>.Failure(TestError).Bind(x => { called = true; return ValueResult<string>.Success("x"); });
        called.Should().BeFalse();
    }

    [Fact]
    public void GenericMatch_OnSuccess_CallsOnSuccess()
    {
        var r = ValueResult<int>.Success(9).Match(v => $"v={v}", _ => "err");
        r.Should().Be("v=9");
    }

    [Fact]
    public void GenericMatch_OnFailure_CallsOnFailure()
    {
        var r = ValueResult<int>.Failure(TestError).Match(v => "ok", e => e.Code);
        r.Should().Be("Validation.Failed");
    }

    [Fact]
    public void GenericTap_OnSuccess_CallsAction()
    {
        int? captured = null;
        ValueResult<int>.Success(99).Tap(v => captured = v);
        captured.Should().Be(99);
    }

    [Fact]
    public void GenericTap_OnFailure_DoesNotCallAction()
    {
        var called = false;
        ValueResult<int>.Failure(TestError).Tap(_ => called = true);
        called.Should().BeFalse();
    }

    [Fact]
    public void GenericTapFailure_OnFailure_CallsAction()
    {
        Error? captured = null;
        ValueResult<int>.Failure(TestError).TapFailure(e => captured = e);
        captured.Should().Be(TestError);
    }

    [Fact]
    public void GenericFinally_AlwaysCalled_OnSuccess()
    {
        var called = false;
        ValueResult<int>.Success(1).Finally(() => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void GenericFinally_AlwaysCalled_OnFailure()
    {
        var called = false;
        ValueResult<int>.Failure(TestError).Finally(() => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void Ensure_OnSuccess_PredicateMet_PassesThrough()
    {
        var r = ValueResult<int>.Success(10).Ensure(x => x > 0, TestError);
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Ensure_OnSuccess_PredicateNotMet_ReturnsFailure()
    {
        var r = ValueResult<int>.Success(-5).Ensure(x => x > 0, TestError);
        r.IsSuccess.Should().BeFalse();
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public void Ensure_OnFailure_NotInvoked()
    {
        var called = false;
        ValueResult<int>.Failure(TestError).Ensure(x => { called = true; return true; }, TestError);
        called.Should().BeFalse();
    }

    [Fact]
    public void GenericRecover_OnFailure_ReturnsSuccessWithRecoveredValue()
    {
        var r = ValueResult<int>.Failure(TestError).Recover(_ => -99);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(-99);
    }

    [Fact]
    public void GenericRecoverWith_OnFailure_CallsRecovery()
    {
        var r = ValueResult<int>.Failure(TestError).RecoverWith(_ => ValueResult<int>.Success(42));
        r.Value.Should().Be(42);
    }

    [Fact]
    public void GenericCombine_BothSuccess_CombinesValues()
    {
        var r = ValueResult<int>.Success(2).Combine(ValueResult<int>.Success(3), (a, b) => a + b);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(5);
    }

    [Fact]
    public void GenericCombine_FirstFailure_ReturnsFirstError()
    {
        var err = Error.Validation("first");
        var r = ValueResult<int>.Failure(err).Combine(ValueResult<int>.Success(1), (a, b) => a + b);
        r.Error.Should().Be(err);
    }

    [Fact]
    public void GenericCombine_SecondFailure_ReturnsSecondError()
    {
        var err = Error.Validation("second");
        var r = ValueResult<int>.Success(1).Combine(ValueResult<int>.Failure(err), (a, b) => a + b);
        r.Error.Should().Be(err);
    }

    [Fact]
    public void BindHeap_OnSuccess_CrossesToResult()
    {
        var r = ValueResult<int>.Success(3).BindHeap(x => Result<string>.Success($"x={x}"));
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be("x=3");
    }

    [Fact]
    public void BindHeap_OnFailure_PropagatesError()
    {
        var r = ValueResult<int>.Failure(TestError).BindHeap(x => Result<string>.Success("ok"));
        r.Error.Should().Be(TestError);
    }

    [Fact]
    public void Flatten_InnerSuccess_ReturnsInner()
    {
        var inner = ValueResult<int>.Success(8);
        var r = ValueResult<ValueResult<int>>.Success(inner).Flatten();
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(8);
    }

    [Fact]
    public void Flatten_OuterFailure_PropagatesError()
    {
        var r = ValueResult<ValueResult<int>>.Failure(TestError).Flatten();
        r.Error.Should().Be(TestError);
    }
}
