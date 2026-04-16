namespace ForEach.Conduit.Tests;

public class ValueResultTests
{
    [Fact]
    public void Success_IsSuccess_True() =>
        ValueResult.Success().IsSuccess.Should().BeTrue();

    [Fact]
    public void Success_Error_IsNull() =>
        ValueResult.Success().Error.Should().BeNull();

    [Fact]
    public void Success_ThrowIfFailure_DoesNotThrow() =>
        ValueResult.Success().Invoking(r => r.ThrowIfFailure()).Should().NotThrow();

    [Fact]
    public void Failure_IsSuccess_False()
    {
        var r = ValueResult.Failure(Error.NotFound("missing"));
        r.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Failure_Error_IsSet()
    {
        var error = Error.NotFound("missing");
        ValueResult.Failure(error).Error.Should().Be(error);
    }

    [Fact]
    public void Failure_ThrowIfFailure_ThrowsInvalidOperationException()
    {
        var r = ValueResult.Failure(Error.Validation("bad"));
        r.Invoking(x => x.ThrowIfFailure())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*failure*");
    }

    [Fact]
    public async Task Success_AsValueTask_ReturnsSuccessResult()
    {
        var r = await ValueResult.Success().AsValueTask();
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Failure_AsValueTask_RetainsError()
    {
        var error = Error.Conflict("dup");
        var r = await ValueResult.Failure(error).AsValueTask();
        r.Error.Should().Be(error);
    }

    [Fact]
    public void GenericSuccess_IsSuccess_True() =>
        ValueResult<int>.Success(42).IsSuccess.Should().BeTrue();

    [Fact]
    public void GenericSuccess_Value_IsSet() =>
        ValueResult<int>.Success(42).Value.Should().Be(42);

    [Fact]
    public void GenericSuccess_Error_IsNull() =>
        ValueResult<string>.Success("hi").Error.Should().BeNull();

    [Fact]
    public void GenericFailure_IsSuccess_False() =>
        ValueResult<int>.Failure(Error.NotFound("x")).IsSuccess.Should().BeFalse();

    [Fact]
    public void GenericFailure_Value_IsDefault() =>
        ValueResult<int>.Failure(Error.NotFound("x")).Value.Should().Be(0);

    [Fact]
    public void GetValueOrThrow_OnSuccess_ReturnsValue() =>
        ValueResult<string>.Success("ok").GetValueOrThrow().Should().Be("ok");

    [Fact]
    public void GetValueOrThrow_OnFailure_Throws()
    {
        var r = ValueResult<int>.Failure(Error.Validation("bad"));
        r.Invoking(x => x.GetValueOrThrow())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*failed result*");
    }

    [Fact]
    public void GetValueOrDefault_OnSuccess_ReturnsValue() =>
        ValueResult<int>.Success(99).GetValueOrDefault(-1).Should().Be(99);

    [Fact]
    public void GetValueOrDefault_OnFailure_ReturnsFallback() =>
        ValueResult<int>.Failure(Error.NotFound("x")).GetValueOrDefault(-1).Should().Be(-1);

    [Fact]
    public async Task GenericSuccess_AsValueTask_ReturnsResult()
    {
        var r = await ValueResult<int>.Success(7).AsValueTask();
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(7);
    }

    [Fact]
    public void Default_ValueResult_Generic_IsSuccess_BecauseErrorIsNull()
    {
        // Error? defaults to null, so IsSuccess = true for default struct
        default(ValueResult<int>).IsSuccess.Should().BeTrue();
    }
}
