namespace HotChocolate.Language;

public class GraphQLCharactersTests
{
    [Fact]
    public void IsLetterOrUnderscore()
    {
        for (var c = 'a'; c <= 'z'; c++)
        {
            Assert.True(GraphQLCharacters.IsLetterOrUnderscore((byte)c));
        }

        for (var c = 'A'; c <= 'Z'; c++)
        {
            Assert.True(GraphQLCharacters.IsLetterOrUnderscore((byte)c));
        }

        Assert.True(GraphQLCharacters.IsLetterOrUnderscore((byte)'_'));
    }

    [Fact]
    public void IsLetterOrDigitOrUnderscore()
    {
        for (var c = 'a'; c <= 'z'; c++)
        {
            Assert.True(GraphQLCharacters.IsLetterOrDigitOrUnderscore((byte)c));
        }

        for (var c = 'A'; c <= 'Z'; c++)
        {
            Assert.True(GraphQLCharacters.IsLetterOrDigitOrUnderscore((byte)c));
        }

        for (var c = '0'; c <= '9'; c++)
        {
            Assert.True(GraphQLCharacters.IsLetterOrDigitOrUnderscore((byte)c));
        }

        Assert.True(GraphQLCharacters.IsLetterOrDigitOrUnderscore((byte)'_'));
    }
}
