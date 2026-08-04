using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionSchemaDefinitionDeprecationTests : FusionTestBase
{
    [Theory]
    [InlineData("@deprecated")]
    [InlineData("@deprecated(reason: \"\")")]
    [InlineData("@deprecated(reason: \"   \")")]
    [InlineData("@deprecated(reason: 5)")]
    public void Create_Should_UseDefaultReason_When_DeprecatedDirectiveHasNoReason(string directive)
    {
        // arrange
        var schemaDocument = CreateSchemaDocument(directive);

        // act
        var schema = FusionSchemaDefinition.Create(schemaDocument);

        // assert
        var field = schema.QueryType.Fields["dragon"];
        Assert.True(field.IsDeprecated);
        Assert.Equal("No longer supported.", field.DeprecationReason);
    }

    [Fact]
    public void Create_Should_UseSpecifiedReason_When_DeprecatedDirectiveHasReason()
    {
        // arrange
        var schemaDocument = CreateSchemaDocument("@deprecated(reason: \"Use dog.\")");

        // act
        var schema = FusionSchemaDefinition.Create(schemaDocument);

        // assert
        var field = schema.QueryType.Fields["dragon"];
        Assert.True(field.IsDeprecated);
        Assert.Equal("Use dog.", field.DeprecationReason);
    }

    [Fact]
    public void Create_Should_NotDeprecateField_When_DeprecatedDirectiveIsAbsent()
    {
        // arrange
        var schemaDocument = ComposeSchemaDocument(SourceSchema);

        // act
        var schema = FusionSchemaDefinition.Create(schemaDocument);

        // assert
        var field = schema.QueryType.Fields["dragon"];
        Assert.False(field.IsDeprecated);
        Assert.Null(field.DeprecationReason);
    }

    // the directive is applied to the composed document so that it reaches the schema
    // builder exactly as written.
    private static DocumentNode CreateSchemaDocument(string deprecatedDirective)
    {
        var compositeSchemaDoc = ComposeSchemaDocument(SourceSchema);

        return Utf8GraphQLParser.Parse(
            compositeSchemaDoc.ToString().Replace(
                "dragon: Dragon",
                $"dragon: Dragon {deprecatedDirective}",
                StringComparison.Ordinal));
    }

    private const string SourceSchema =
        """
        type Query { dragon: Dragon }

        type Dragon { name: String }
        """;
}
