namespace HotChocolate;

public class OptionalTests
{
    [Fact]
    public void Optional_Is_Not_Set()
    {
        // arrange
        // act
        var optional = new Optional<string>();

        // assert
        Assert.False(optional.HasValue);
        Assert.True(optional.IsEmpty);
        Assert.Null(optional.Value);
    }

    [Fact]
    public void Optional_Is_Set_To_Value()
    {
        // arrange
        // act
        Optional<string> optional = "abc";

        // assert
        Assert.True(optional.HasValue);
        Assert.False(optional.IsEmpty);
        Assert.Equal("abc", optional.Value);
    }

    [Fact]
    public void Optional_Is_Set_To_Null()
    {
        // arrange
        // act
        Optional<string?> optional = null;

        // assert
        Assert.True(optional.HasValue);
        Assert.False(optional.IsEmpty);
        Assert.Null(optional.Value);
    }

    [Fact]
    public void Optional_Equals_True()
    {
        // arrange
        Optional<string> a = "abc";
        Optional<string> b = "abc";

        // act
        var result = a.Equals(b);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void Optional_Equals_True_2()
    {
        // arrange
        Optional<string> a = "abc";
        var b = "abc";

        // act
        var result = a.Equals(b);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void Optional_Equals_False()
    {
        // arrange
        Optional<string> a = "abc";
        Optional<string> b = "def";

        // act
        var result = a.Equals(b);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void Optional_Equals_Operator_True()
    {
        // arrange
        Optional<string> a = "abc";
        Optional<string> b = "abc";

        // act
        var result = a == b;

        // assert
        Assert.True(result);
    }

    [Fact]
    public void Optional_Equals_Operator_True_2()
    {
        // arrange
        Optional<string> a = "abc";
        var b = "abc";

        // act
        var result = a == b;

        // assert
        Assert.True(result);
    }

    [Fact]
    public void Optional_Equals_Operator_False()
    {
        // arrange
        Optional<string> a = "abc";
        Optional<string> b = "def";

        // act
        var result = a == b;

        // assert
        Assert.False(result);
    }

    [Fact]
    public void Optional_Not_Equals_Operator_True()
    {
        // arrange
        Optional<string> a = "abc";
        Optional<string> b = "abc";

        // act
        var result = a != b;

        // assert
        Assert.False(result);
    }

    [Fact]
    public void Optional_Not_Equals_Operator_True_2()
    {
        // arrange
        Optional<string> a = "abc";
        var b = "abc";

        // act
        var result = a != b;

        // assert
        Assert.False(result);
    }

    [Fact]
    public void Optional_Not_Equals_Operator_False()
    {
        // arrange
        Optional<string> a = "abc";
        Optional<string> b = "def";

        // act
        var result = a != b;

        // assert
        Assert.True(result);
    }

    [Fact]
    public void Optional_From_Value_Equals()
    {
        Optional<int> a = 1;
        var b = Optional<int>.From(a);

        Assert.True(a.HasValue);
        Assert.True(b.HasValue);
        Assert.Equal(a.Value, b.Value);
    }

    [Fact]
    public void Optional_From_Struct_Is_Not_Set()
    {
        var emptyOptional = new Optional<int?>();
        var fromEmptyOptional = Optional<int>.From(emptyOptional);

        Assert.False(fromEmptyOptional.HasValue);
        Assert.True(fromEmptyOptional.IsEmpty);
    }

    [Fact]
    public void Optional_From_DefaultValueAttribute_Provided()
    {
        const int defaultValue = 500;
        var a = Optional<int>.Empty(defaultValue);
        var b = Optional<int>.From(a);

        Assert.False(a.HasValue);
        Assert.False(b.HasValue);
        Assert.Equal(defaultValue, b.Value);
    }
}
