using System.Text.Json;

namespace HotChocolate.Language;

public class JsonValueParserTests
{
    [Fact]
    public void Parse_JsonElement_StringWithEscapedQuotes_IsUnescaped()
    {
        // arrange
        using var document = JsonDocument.Parse(
            """
            "tag:\"type_portable-lamp\""
            """);
        var parser = new JsonValueParser();

        // act
        var value = parser.Parse(document.RootElement);

        // assert
        var stringValue = Assert.IsType<StringValueNode>(value);
        Assert.Equal("tag:\"type_portable-lamp\"", stringValue.Value);
    }
}
