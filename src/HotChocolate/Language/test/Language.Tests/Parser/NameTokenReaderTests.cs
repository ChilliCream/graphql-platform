using System.Text;

namespace HotChocolate.Language;

public class NameTokenReaderTests
{
    [InlineData("    \nhelloWorld_123")]
    [InlineData("    \nhelloWorld_123\n     ")]
    [InlineData("helloWorld_123\n     ")]
    [InlineData("helloWorld_123")]
    [Theory]
    public void ReadToken(string sourceText)
    {
        // arrange
        const string nameTokenValue = "helloWorld_123";
        var source = Encoding.UTF8.GetBytes(sourceText);
        var reader = new Utf8GraphQLReader(source);

        // act
        reader.Read();

        // assert
        Assert.Equal(TokenKind.Name, reader.Kind);
        Assert.Equal(nameTokenValue, reader.GetName());
    }
}
