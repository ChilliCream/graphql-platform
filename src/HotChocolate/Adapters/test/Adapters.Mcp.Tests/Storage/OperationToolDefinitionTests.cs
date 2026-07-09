using HotChocolate.Language;

namespace HotChocolate.Adapters.Mcp.Storage;

public sealed class OperationToolDefinitionTests
{
    [Theory]
    [InlineData("valid")]
    [InlineData("valid_name")]
    [InlineData("valid.name")]
    [InlineData("valid-name")]
    public void OperationToolDefinition_WithValidName_Succeeds(string name)
    {
        // arrange & act
        var exception =
            Record.Exception(
                () =>
                    new OperationToolDefinition(Utf8GraphQLParser.Parse(OperationDocument))
                    {
                        Name = name
                    });

        // assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid🔧")]
    [InlineData("invalid name")]
    [InlineData("invalid/name")]
    [InlineData("invalid\\name")]
    [InlineData("invalid", 20)] // 7 characters repeated 20 times = 140 characters
    public void OperationToolDefinition_WithInvalidName_ThrowsArgumentException(
        string name,
        int repeat = 0)
    {
        // arrange
        if (repeat > 0)
        {
            name = string.Concat(Enumerable.Repeat(name, repeat));
        }

        // act
        var exception =
            Record.Exception(
                () =>
                    new OperationToolDefinition(Utf8GraphQLParser.Parse(OperationDocument))
                    {
                        Name = name
                    });

        // assert
        Assert.IsType<ArgumentException>(exception);
        Assert.Equal(
            $"The tool name '{name}' is invalid. Tool names must match the regular expression "
            + "'^[A-Za-z0-9_.-]{1,128}\\z'.",
            exception.Message);
    }

    private const string OperationDocument = "query GetUsers { users { id } }";
}
