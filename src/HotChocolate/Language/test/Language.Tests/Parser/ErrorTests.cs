using Xunit;

namespace HotChocolate.Language;

public class ErrorTests
{
    [Fact]
    public void Missing_EndBrace_For_SelectionSet()
    {
        var ex = Assert.Throws<SyntaxException>(() => Utf8GraphQLParser.Parse("query { x"));
        Assert.Equal("Expected a `RightBrace`-token, but found a `EndOfFile`-token.", ex.Message);
    }
}
