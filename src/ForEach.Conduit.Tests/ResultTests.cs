namespace ForEach.Conduit.Tests;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccess_True() =>
        Result.Success().IsSuccess.Should().BeTrue();

    [Fact]
    public void Success_Error_IsNull() =>
        Result.Success().Error.Should().BeNull();

    [Fact]
    public void Success_ThrowIfFailure_DoesNotThrow() =>
        Result.Success().Invoking(r => r.ThrowIfFailure()).Should().NotThrow();

    [Fact]
    public void Failure_IsSuccess_False() =>
        Result.Failure(Error.NotFound("x")).IsSuccess.Should().BeFalse();

    [Fact]
    public void Failure_Error_IsSet()
    {
        var error = Error.Conflict("dup");
        Result.Failure(error).Error.Should().Be(error);
    }

    [Fact]
    public void Failure_ThrowIfFailure_ThrowsInvalidOperationException()
    {
        Result.Failure(Error.Validation("bad"))
            .Invoking(r => r.ThrowIfFailure())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*failure*");
    }

    [Fact]
    public void Success_IsReferenceType() =>
        Result.Success().Should().NotBeNull().And.BeOfType<Result>();

    [Fact]
    public void GenericSuccess_IsSuccess_True() =>
        Result<int>.Success(42).IsSuccess.Should().BeTrue();

    [Fact]
    public void GenericSuccess_Value_IsSet() =>
        Result<int>.Success(42).Value.Should().Be(42);

    [Fact]
    public void GenericSuccess_Error_IsNull() =>
        Result<string>.Success("hi").Error.Should().BeNull();

    [Fact]
    public void GenericFailure_IsSuccess_False() =>
        Result<int>.Failure(Error.NotFound("x")).IsSuccess.Should().BeFalse();

    [Fact]
    public void GenericFailure_Value_IsNull() =>
        Result<string>.Failure(Error.NotFound("x")).Value.Should().BeNull();

    [Fact]
    public void GetValueOrThrow_OnSuccess_ReturnsValue() =>
        Result<int>.Success(5).GetValueOrThrow().Should().Be(5);

    [Fact]
    public void GetValueOrThrow_OnFailure_Throws()
    {
        Result<int>.Failure(Error.Validation("bad"))
            .Invoking(r => r.GetValueOrThrow())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*failed result*");
    }

    [Fact]
    public void GetValueOrDefault_OnSuccess_ReturnsValue() =>
        Result<int>.Success(10).GetValueOrDefault(0).Should().Be(10);

    [Fact]
    public void GetValueOrDefault_OnFailure_ReturnsFallback() =>
        Result<int>.Failure(Error.NotFound("x")).GetValueOrDefault(-99).Should().Be(-99);
}
