using System.Text;
using HotChocolate.Fusion.Utilities;

namespace HotChocolate.Fusion;

public class DefaultNodeIdParserTests
{
    [Theory]
    [InlineData("Product:123", "Product")]
    [InlineData("Product:123:456", "Product")]
    [InlineData("Product\nd123", "Product")]
    [InlineData("Product\nd123:456", "Product")]
    public void Test(string unencodedId, string typeName)
    {
        // arrange
        var parser = new DefaultNodeIdParser();
        var encodedId = Convert.ToBase64String(Encoding.UTF8.GetBytes(unencodedId));

        // act
        var parsedTypeName = parser.ParseTypeName(encodedId);

        // assert
        Assert.Equal(typeName, parsedTypeName);
    }
}
