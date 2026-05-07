namespace HotChocolate.Adapters.Mcp.Extensions;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("hello", "hello")]
    [InlineData("HelloWorld", "hello_world")]
    [InlineData("helloWorld", "hello_world")]
    [InlineData("hello_world", "hello_world")]
    [InlineData("HTMLParser", "html_parser")]
    [InlineData("GetUserById", "get_user_by_id")]
    [InlineData("UserId123", "user_id123")]
    [InlineData("user name", "user_name")]
    [InlineData("ABC", "abc")]
    [InlineData("A", "a")]
    [InlineData("___leading", "___leading")]
    [InlineData("alreadyAllLowercase", "already_all_lowercase")]
    [InlineData("hello.world", "hello_world")]
    [InlineData("hello/world", "hello_world")]
    [InlineData("123abc", "123abc")]
    [InlineData("v2Endpoint", "v2endpoint")]
    public void ToSnakeCase_Should_ReturnSnakeCase_When_GivenInput(string input, string expected)
    {
        // act
        var result = input.ToSnakeCase();

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToSnakeCase_Should_ReturnNull_When_InputIsNull()
    {
        // act
        var result = ((string)null!).ToSnakeCase();

        // assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("hello", "hello")]
    [InlineData("HelloWorld", "hello-world")]
    [InlineData("helloWorld", "hello-world")]
    [InlineData("hello_world", "hello-world")]
    [InlineData("hello world", "hello-world")]
    [InlineData("hello-world", "hello-world")]
    [InlineData("HTMLParser", "html-parser")]
    [InlineData("UserId123", "user-id123")]
    [InlineData("ABC", "abc")]
    [InlineData("A", "a")]
    [InlineData("hello   world", "hello-world")]
    [InlineData("alreadyAllLowercase", "already-all-lowercase")]
    [InlineData("hello.world", "hello-world")]
    [InlineData("123abc", "123abc")]
    [InlineData("v2Endpoint", "v2endpoint")]
    public void ToKebabCase_Should_ReturnKebabCase_When_GivenInput(string input, string expected)
    {
        // act
        var result = input.ToKebabCase();

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToKebabCase_Should_ReturnNull_When_InputIsNull()
    {
        // act
        var result = ((string)null!).ToKebabCase();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void ToSnakeCase_And_ToKebabCase_Should_Agree_On_Boundary_For_Same_Input()
    {
        // arrange
        const string input = "GetUserById";

        // act
        var snake = input.ToSnakeCase();
        var kebab = input.ToKebabCase();

        // assert
        Assert.Equal("get_user_by_id", snake);
        Assert.Equal("get-user-by-id", kebab);
        Assert.Equal(snake.Replace('_', '-'), kebab);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("hello", "hello")]
    [InlineData("HelloWorld", "Hello World")]
    [InlineData("helloWorld", "hello World")]
    [InlineData("HTMLParser", "H T M L Parser")]
    [InlineData("hello world", "hello world")]
    [InlineData("A", "A")]
    [InlineData("AB", "A B")]
    [InlineData("Already Spaced", "Already Spaced")]
    public void InsertSpaceBeforeUpperCase_Should_InsertSpace_When_UpperCaseFollowsOther(
        string input,
        string expected)
    {
        // act
        var result = input.InsertSpaceBeforeUpperCase();

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void InsertSpaceBeforeUpperCase_Should_ReturnNull_When_InputIsNull()
    {
        // act
        var result = ((string)null!).InsertSpaceBeforeUpperCase();

        // assert
        Assert.Null(result);
    }
}
