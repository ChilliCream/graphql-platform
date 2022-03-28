using Xunit;

namespace HotChocolate.Language;

public class Foo
{
    [Fact]
    public void IsLetterOrUnderscore()
    {
        for (char c = 'a'; c <= 'z'; c++)
        {
            Assert.True(GraphQLConstants.IsLetterOrUnderscore((byte)c));
        }

        for (char c = 'A'; c <= 'Z'; c++)
        {
            Assert.True(GraphQLConstants.IsLetterOrUnderscore((byte)c));
        }

        Assert.True(GraphQLConstants.IsLetterOrUnderscore((byte)'_'));
    }

    [Fact]
    public void IsLetterOrDigitOrUnderscore()
    {
        for (char c = 'a'; c <= 'z'; c++)
        {
            Assert.True(GraphQLConstants.IsLetterOrDigitOrUnderscore((byte)c));
        }

        for (char c = 'A'; c <= 'Z'; c++)
        {
            Assert.True(GraphQLConstants.IsLetterOrDigitOrUnderscore((byte)c));
        }

        for (char c = '0'; c <= '9'; c++)
        {
            Assert.True(GraphQLConstants.IsLetterOrDigitOrUnderscore((byte)c));
        }

        Assert.True(GraphQLConstants.IsLetterOrDigitOrUnderscore((byte)'_'));
    }
}
