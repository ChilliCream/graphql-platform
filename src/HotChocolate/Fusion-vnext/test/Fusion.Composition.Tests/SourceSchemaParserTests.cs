using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaParserTests
{
    [Fact]
    public void Parse_SourceSchemaInvalidGraphQL_ReturnsError()
    {
        // arrange
        var sourceSchemaText = new SourceSchemaText("A", "type Query {");
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal("Source schema parsing failed.", result.Errors[0].Message);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: Expected a `Name`-token, "
            + "but found a `EndOfFile`-token..",
            entry.Message);
        Assert.Equal("A", entry.Schema?.Name);
    }

    [Fact]
    public void Parse_SourceSchemaWithSchemaName_SetsSchemaName()
    {
        // arrange
        var sourceSchemaText = new SourceSchemaText("A", "schema { }");
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        Assert.Equal("A", result.Value.Name);
    }

    [Fact]
    public void Parse_SourceSchemaWithSchemaErrors_ReturnsErrors()
    {
        // arrange
        var sourceSchemaText = new SourceSchemaText("A", "type Empty { }");
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal("Source schema parsing failed.", result.Errors[0].Message);
        var entry = Assert.Single(log);
        Assert.Equal(
            "The Object type 'Empty' must define one or more fields. (Schema: 'A')",
            entry.Message);
        Assert.Equal("HCV0001", entry.Code);
    }
}
