using Xunit;

namespace GreenDonut;

public class ResultTests
{
    [Fact(DisplayName = "Equals: Should return false if comparing error with value")]
    public void EqualsErrorValue()
    {
        // arrange
        Result<string> error = new Exception("Foo");
        Result<string> value = "Bar";

        // act
        var result = error.Equals(value);

        // assert
        Assert.False(result);
    }

    [Fact(DisplayName = "Equals: Should return false if the error is not equal")]
    public void EqualsDifferentError()
    {
        // arrange
        Result<string> errorA = new Exception("Foo");
        Result<string> errorB = new Exception("Bar");

        // act
        var result = errorA.Equals(errorB);

        // assert
        Assert.False(result);
    }

    [Fact(DisplayName = "Equals: Should return true if the error is equal")]
    public void EqualsSameError()
    {
        // arrange
        Result<string> error = new Exception("Foo");

        // act
        var result = error.Equals(error);

        // assert
        Assert.True(result);
    }

    [Fact(DisplayName = "Equals: Should return false if the error is not equal")]
    public void EqualsDifferentValue()
    {
        // arrange
        Result<string> valueA = "Foo";
        Result<string> valueB = "Bar";

        // act
        var result = valueA.Equals(valueB);

        // assert
        Assert.False(result);
    }

    [Fact(DisplayName = "Equals: Should return true if the value is equal")]
    public void EqualsSameValue()
    {
        // arrange
        Result<string> value = "Foo";

        // act
        var result = value.Equals(value);

        // assert
        Assert.True(result);
    }

    [Fact(DisplayName = "Equals: Should return false if object is null")]
    public void EqualsObjectNull()
    {
        // arrange
        Result<string> value = "Foo";

        // act
        var result = value.Equals(default(object));

        // assert
        Assert.False(result);
    }

    [Fact(DisplayName = "Equals: Should return false if object type is different")]
    public void EqualsObjectNotEqual()
    {
        // arrange
        object obj = "Foo";
        Result<string> value = "Bar";

        // act
        var result = value.Equals(obj);

        // assert
        Assert.False(result);
    }

    [Fact(DisplayName = "Equals: Should return false if object value is different")]
    public void EqualsObjectValueNotEqual()
    {
        // arrange
        object obj = (Result<string>)"Foo";
        Result<string> value = "Bar";

        // act
        var result = value.Equals(obj);

        // assert
        Assert.False(result);
    }

    [Fact(DisplayName = "Equals: Should return true if object value is equal")]
    public void EqualsObjectValueEqual()
    {
        // arrange
        object obj = (Result<string>)"Foo";
        Result<string> value = "Foo";

        // act
        var result = value.Equals(obj);

        // assert
        Assert.True(result);
    }

    [Fact(DisplayName = "GetHashCode: Should be consistent")]
    public void GetHashCodeEmpty()
    {
        // arrange
        // act
        Result<string?> result1 = default(string);
        Result<string?> result2 = default(string);

        // assert
        Assert.Equal(result2.GetHashCode(), result1.GetHashCode());
    }

    [Fact(DisplayName = "GetHashCode: Should return a hash code for value")]
    public void GetHashCodeValue()
    {
        // arrange
        var value = "Foo";

        // act
        Result<string> result1 = value;
        Result<string> result2 = value;

        // assert
        Assert.Equal(result2.GetHashCode(), result1.GetHashCode());
    }

    [Fact(DisplayName = "GetHashCode: Should return a hash code for error")]
    public void GetHashCodeError()
    {
        // arrange
        var error = new Exception();

        // act
        Result<string> result1 = error;
        Result<string> result2 = error;

        // assert
        Assert.Equal(result2.GetHashCode(), result1.GetHashCode());
    }

    [Fact(DisplayName = "ImplicitReject: Should return a resolved Result if error is null")]
    public void ImplicitRejectErrorIsNull()
    {
        // arrange
        // act
        Result<object> result = default(Exception);

        // assert
        Assert.Equal(ResultKind.Value, result.Kind);
        Assert.Null(result.Error);
        Assert.Null(result.Value);
    }

    [Fact(DisplayName = "ImplicitReject: Should return a rejected Result")]
    public void ImplicitReject()
    {
        // arrange
        var errorMessage = "Foo";
        var error = new Exception(errorMessage);

        // act
        Result<string> result = error;

        // assert
        Assert.Equal(ResultKind.Error, result.Kind);
        Assert.Equal(error, result.Error);
        Assert.Null(result.Value);
    }

    [Fact(DisplayName = "ExplicitReject: Should return a rejected Result")]
    public void ExplicitReject()
    {
        // arrange
        var errorMessage = "Foo";
        var error = new Exception(errorMessage);

        // act
        var result = Result<string>.Reject(error);

        // assert
        Assert.Equal(ResultKind.Error, result.Kind);
        Assert.Equal("Foo", result.Error?.Message);
        Assert.Null(result.Value);
    }

    [InlineData(null)]
    [InlineData("Foo")]
    [Theory(DisplayName = "ImplicitResolve: Should return a resolved Result")]
    public void ImplicitResolve(string? value)
    {
        // act
        Result<string?> result = value;

        // assert
        Assert.Equal(ResultKind.Value, result.Kind);
        Assert.Null(result.Error);
        Assert.Equal(value, result);
    }

    [InlineData(null)]
    [InlineData("Foo")]
    [Theory(DisplayName = "ExplicitResolve: Should return a resolved Result")]
    public void ExplicitResolve(string? value)
    {
        // act
        var result = Result<string?>.Resolve(value);

        // assert
        Assert.Equal(ResultKind.Value, result.Kind);
        Assert.Null(result.Error);
        Assert.Equal(value, result.Value);
    }

    [Fact(DisplayName = "ExplicitResolve: Should return a resolved Result of list")]
    public void ExplicitResolveList()
    {
        // arrange
        var value = new[] { "Foo", "Bar", "Baz", };

        // act
        var result = Result<IReadOnlyCollection<string>>.Resolve(value);

        // assert
        Assert.Equal(ResultKind.Value, result.Kind);
        Assert.Null(result.Error);
        Assert.Equal(value, result.Value);
    }
}
