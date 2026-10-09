using System.Text.Json;

namespace HotChocolate.Fusion;

public sealed class LocalSourceSchemaTests
{
    [Fact]
    public void Constructor_Should_ThrowArgumentException_When_UrlOverrideIsRelative()
    {
        // arrange
        using var settings = JsonDocument.Parse("""{ "name": "Products" }""");
        var schema = new SourceSchemaText("Products", "type Query { product: String }");
        var relativeUrlOverride = new Uri("/graphql", UriKind.Relative);

        // act
        void Act() => _ = new LocalSourceSchema(schema, settings, relativeUrlOverride);

        // assert
        var exception = Assert.Throws<ArgumentException>(Act);
        Assert.Equal("urlOverride", exception.ParamName);
    }
}
