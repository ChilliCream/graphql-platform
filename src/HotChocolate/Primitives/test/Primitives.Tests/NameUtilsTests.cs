using HotChocolate.Utilities;

namespace HotChocolate;

public class NameUtilsTests
{
    [Theory]
    [InlineData("1Bar")]
    [InlineData("$Bar")]
    [InlineData("1_Bar")]
    [InlineData("B/ar")]
    [InlineData("B+ar")]
    public void InvalidName(string name)
    {
        var message = Assert.Throws<ArgumentException>(() => name.EnsureGraphQLName()).Message;
        Assert.Equal(
            $"`{name}` is not a valid GraphQL name.{Environment.NewLine}"
            + $"https://spec.graphql.org/October2021/#sec-Names{Environment.NewLine}"
            + $" (Parameter 'name')",
            message);
    }

    [Theory]
    [InlineData("_1Bar")]
    [InlineData("Bar")]
    [InlineData("_Bar")]
    [InlineData("Bar123")]
    [InlineData("Bar_123")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    [InlineData("abcdefghijklmnopqrstuvwxyz")]
    [InlineData("_1234567890")]
    public void ValidName(string name)
    {
        name.EnsureGraphQLName();
    }
}
