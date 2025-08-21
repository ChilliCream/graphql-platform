using System.Text;

namespace HotChocolate.Fusion.Execution;

public class DefaultNodeIdParserTests
{
    [Theory]
    [InlineData("Product:123", "Product")]
    [InlineData("Product:123:456", "Product")]
    [InlineData("Product\nd123", "Product")]
    [InlineData("Product\nd123:456", "Product")]
    public void ValidId(string unencodedId, string typeName)
    {
        // arrange
        var parser = new DefaultNodeIdParser();
        var encodedId = Convert.ToBase64String(Encoding.UTF8.GetBytes(unencodedId));

        // act
        var success = parser.TryParseTypeNameFromId(encodedId, out var parsedTypeName);

        // assert
        Assert.True(success);
        Assert.Equal(typeName, parsedTypeName);
    }

    [Theory]
    [InlineData("non-base-64-id")]
    [InlineData("aW52YWxpZA==")] // Base64, but neither delimiter
    [InlineData("UHJvZHVjdDo=")] // Delimiter is last character
    public void InvalidId(string rawId)
    {
        // arrange
        var parser = new DefaultNodeIdParser();

        // act
        var success = parser.TryParseTypeNameFromId(rawId, out _);

        // assert
        Assert.False(success);
    }
}
