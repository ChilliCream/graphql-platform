using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaParserTests
{
    [Fact]
    public void Parse_SourceSchemasInvalidGraphQL_ReturnsError()
    {
        // arrange
        var schemas =
            ImmutableArray.Create(
                new SourceSchemaText("A", "type Query {"),
                new SourceSchemaText("B", "type Query {"));
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(schemas, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal("Source schema parsing failed.", result.Errors[0].Message);
        Assert.Collection(
            log,
            e =>
            {
                Assert.Equal(
                    "Invalid GraphQL in source schema. Exception message: Expected a `Name`-token, "
                    + "but found a `EndOfFile`-token..",
                    e.Message);
                Assert.Equal("A", e.Schema?.Name);
            },
            e =>
            {
                Assert.Equal(
                    "Invalid GraphQL in source schema. Exception message: Expected a `Name`-token, "
                    + "but found a `EndOfFile`-token..",
                    e.Message);
                Assert.Equal("B", e.Schema?.Name);
            });
    }

    [Fact]
    public void Parse_SourceSchemasWithSchemaName_SetsSchemaName()
    {
        // arrange
        var schemas =
            ImmutableArray.Create(
                new SourceSchemaText("A", "schema { }"),
                new SourceSchemaText("B", "schema { }"));
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(schemas, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal("A", result.Value[0].Name);
        Assert.Equal("B", result.Value[1].Name);
    }

    [Fact]
    public void Parse_SourceSchemasWithSchemaErrors_ReturnsErrors()
    {
        // arrange
        var schemas =
            ImmutableArray.Create(
                new SourceSchemaText(
                    "A",
                    """
                    type Query {
                       foo: URI
                    }
                    """),
                new SourceSchemaText(
                    "B",
                    "type Empty { }"));
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(schemas, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal("Source schema parsing failed.", result.Errors[0].Message);
        Assert.Collection(
            log,
            e =>
            {
                Assert.Equal(
                    "The type 'URI' of field 'Query.foo' is not defined in the schema. (Schema: 'A')",
                    e.Message);
                Assert.Equal("HCV0021", e.Code);
            },
            e =>
            {
                Assert.Equal(
                    "The Object type 'Empty' must define one or more fields. (Schema: 'B')",
                    e.Message);
                Assert.Equal("HCV0001", e.Code);
            });
    }
}
