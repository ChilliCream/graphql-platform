using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaParserTests
{
    [Fact]
    public void Parse_SourceSchemasInvalidGraphQL_ReturnsError()
    {
        // arrange
        var schemas = ImmutableArray.Create("type Query {");
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(schemas, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal("Source schema parsing failed.", result.Errors[0].Message);
        Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: Expected a `Name`-token, but "
            + "found a `EndOfFile`-token..",
            log.First().Message);
    }

    [Fact]
    public void Parse_SourceSchemasWithSchemaNameDirective_SetsSchemaName()
    {
        // arrange
        var schemas = ImmutableArray.Create("""schema @schemaName(value: "Example") { }""");
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(schemas, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Example", result.Value[0].Name);
    }
}
