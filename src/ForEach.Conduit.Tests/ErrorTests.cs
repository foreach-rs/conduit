namespace ForEach.Conduit.Tests;

public class ErrorTests
{
    [Fact]
    public void DefaultError_HasNullCode() =>
        default(Error).Code.Should().BeNull();

    [Fact]
    public void DefaultError_HasNullMessage() =>
        default(Error).Message.Should().BeNull();

    [Fact]
    public void DefaultError_IsValid_ReturnsFalse() =>
        default(Error).IsValid.Should().BeFalse();

    [Fact]
    public void Constructor_SetsCodeAndMessage()
    {
        var e = new Error("CODE", "message");
        e.Code.Should().Be("CODE");
        e.Message.Should().Be("message");
    }

    [Fact]
    public void Constructor_IsValid_ReturnsTrue() =>
        new Error("CODE", "msg").IsValid.Should().BeTrue();

    [Fact]
    public void Constructor_ExceptionIsNull_WhenNotProvided() =>
        new Error("CODE", "msg").Exception.Should().BeNull();

    [Fact]
    public void Constructor_SetsException()
    {
        var ex = new Exception("boom");
        new Error("CODE", "msg", ex).Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void Constructor_NullCode_Throws() =>
        ((Action)(() => _ = new Error(null!, "msg"))).Should().Throw<ArgumentNullException>();

    [Fact]
    public void Constructor_NullMessage_Throws() =>
        ((Action)(() => _ = new Error("CODE", null!))).Should().Throw<ArgumentNullException>();

    [Fact]
    public void NotFound_HasCorrectCode() =>
        Error.NotFound("not here").Code.Should().Be("NotFound");

    [Fact]
    public void NotFound_HasCorrectMessage() =>
        Error.NotFound("not here").Message.Should().Be("not here");

    [Fact]
    public void Validation_HasCorrectCode() =>
        Error.Validation("bad input").Code.Should().Be("Validation.Failed");

    [Fact]
    public void Conflict_HasCorrectCode() =>
        Error.Conflict("already exists").Code.Should().Be("Conflict");

    [Fact]
    public void Unauthorized_HasCorrectCode() =>
        Error.Unauthorized("no access").Code.Should().Be("Unauthorized");

    [Fact]
    public void FromException_UsesTypeNameAsDefaultCode()
    {
        var ex = new InvalidOperationException("oops");
        var err = Error.FromException(ex);
        err.Code.Should().Be("InvalidOperationException");
        err.Message.Should().Be("oops");
        err.Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void FromException_UsesCustomCode()
    {
        var ex = new Exception("boom");
        Error.FromException(ex, "CUSTOM").Code.Should().Be("CUSTOM");
    }

    [Fact]
    public void Aggregate_WithErrors_HasCorrectCode()
    {
        var errors = new[] { Error.Validation("a"), Error.Validation("b") };
        Error.Aggregate(errors).Code.Should().Be("Validation.Multiple");
    }

    [Fact]
    public void Aggregate_WithErrors_MessageIncludesCount()
    {
        var errors = new[] { Error.Validation("a"), Error.Validation("b") };
        Error.Aggregate(errors).Message.Should().Contain("2 validation error(s)");
    }

    [Fact]
    public void Aggregate_WithErrors_SetsInnerErrors()
    {
        var errors = new[] { Error.Validation("a"), Error.Validation("b") };
        var agg = Error.Aggregate(errors);
        agg.InnerErrors.Should().HaveCount(2);
        agg.InnerErrors![0].Should().Be(errors[0]);
        agg.InnerErrors![1].Should().Be(errors[1]);
    }

    [Fact]
    public void Aggregate_WithMessages_ProducesValidationErrors()
    {
        var agg = Error.Aggregate(["msg1", "msg2"]);
        agg.Code.Should().Be("Validation.Multiple");
        agg.InnerErrors.Should().HaveCount(2);
        agg.InnerErrors![0].Code.Should().Be("Validation.Failed");
    }

    [Fact]
    public void Aggregate_WithEmptyList_Throws()
    {
        var act = () => Error.Aggregate(Array.Empty<Error>());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Aggregate_WithNullList_Throws()
    {
        var act = () => Error.Aggregate((IReadOnlyList<Error>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Equals_SameCodeAndMessage_ReturnsTrue()
    {
        var a = new Error("CODE", "msg");
        var b = new Error("CODE", "msg");
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentCode_ReturnsFalse()
    {
        var a = new Error("CODE1", "msg");
        var b = new Error("CODE2", "msg");
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentMessage_ReturnsFalse()
    {
        var a = new Error("CODE", "msg1");
        var b = new Error("CODE", "msg2");
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_SameCodeAndMessage_ReturnsTrue()
    {
        var a = new Error("CODE", "msg");
        var b = new Error("CODE", "msg");
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_DifferentCode_ReturnsTrue()
    {
        var a = new Error("CODE1", "msg");
        var b = new Error("CODE2", "msg");
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_EqualErrors_HaveSameHash()
    {
        var a = new Error("CODE", "msg");
        var b = new Error("CODE", "msg");
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsCodeAndMessage()
    {
        var e = new Error("NOT_FOUND", "item missing");
        e.ToString().Should().Contain("NOT_FOUND").And.Contain("item missing");
    }
}
