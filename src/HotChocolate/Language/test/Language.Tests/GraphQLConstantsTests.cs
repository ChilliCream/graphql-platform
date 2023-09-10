using Xunit;

namespace HotChocolate.Language;

public class GraphQLConstantsTests
{
    [Fact]
    public void IsLetterOrUnderscore()
    {
        for (var c = 'a'; c <= 'z'; c++)
        {
            Assert.True(GraphQLConstants.IsLetterOrUnderscore((byte)c));
        }

        for (var c = 'A'; c <= 'Z'; c++)
        {
            Assert.True(GraphQLConstants.IsLetterOrUnderscore((byte)c));
        }

        Assert.True(GraphQLConstants.IsLetterOrUnderscore((byte)'_'));
    }

    [Fact]
    public void IsLetterOrDigitOrUnderscore()
    {
        for (var c = 'a'; c <= 'z'; c++)
        {
            Assert.True(GraphQLConstants.IsLetterOrDigitOrUnderscore((byte)c));
        }

        for (var c = 'A'; c <= 'Z'; c++)
        {
            Assert.True(GraphQLConstants.IsLetterOrDigitOrUnderscore((byte)c));
        }

        for (var c = '0'; c <= '9'; c++)
        {
            Assert.True(GraphQLConstants.IsLetterOrDigitOrUnderscore((byte)c));
        }

        Assert.True(GraphQLConstants.IsLetterOrDigitOrUnderscore((byte)'_'));
    }
}
