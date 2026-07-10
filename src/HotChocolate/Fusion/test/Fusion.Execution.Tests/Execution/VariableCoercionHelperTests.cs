using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public class VariableCoercionHelperTests : FusionTestBase
{
    [Fact]
    public void Coerce_String_WithEscapedQuotes_IsUnescaped()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new VariableDefinitionNode(
                null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("String"),
                null,
                Array.Empty<DirectiveNode>())
        };

        using var variableValues = JsonDocument.Parse(
            """
            {
              "abc": "tag:\"type_portable-lamp\""
            }
            """);

        // act
        var success = VariableCoercionHelper.TryCoerceVariableValues(
            new MockFeatureProvider(),
            schema,
            variableDefinitions,
            variableValues.RootElement,
            out var coercedVariableValues,
            out var error);

        // assert
        Assert.True(success, error?.Message);
        Assert.Null(error);
        Assert.NotNull(coercedVariableValues);

        var stringValue = Assert.IsType<StringValueNode>(coercedVariableValues["abc"].Value);
        Assert.Equal("tag:\"type_portable-lamp\"", stringValue.Value);
    }

    [Fact]
    public void Single_Value_Can_Be_Coerced_Into_List_Variable()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new ListTypeNode(new NamedTypeNode("String")),
                null,
                Array.Empty<DirectiveNode>())
        };

        using var variableValues = JsonDocument.Parse("""{"abc": "xyz"}""");

        // act
        var success = VariableCoercionHelper.TryCoerceVariableValues(
            new MockFeatureProvider(),
            schema,
            variableDefinitions,
            variableValues.RootElement,
            out var coercedVariableValues,
            out var error);

        // assert
        Assert.True(success, error?.Message);
        Assert.Null(error);
        Assert.NotNull(coercedVariableValues);

        var list = Assert.IsType<ListValueNode>(coercedVariableValues["abc"].Value);
        var item = Assert.Single(list.Items);
        var stringValue = Assert.IsType<StringValueNode>(item);
        Assert.Equal("xyz", stringValue.Value);
    }

    [Fact]
    public void Error_When_Single_Value_Type_Does_Not_Match_List_Element_Type()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new ListTypeNode(new NamedTypeNode("Int")),
                null,
                Array.Empty<DirectiveNode>())
        };

        using var variableValues = JsonDocument.Parse("""{"abc": "xyz"}""");

        // act
        var success = VariableCoercionHelper.TryCoerceVariableValues(
            new MockFeatureProvider(),
            schema,
            variableDefinitions,
            variableValues.RootElement,
            out var coercedVariableValues,
            out var error);

        // assert
        Assert.False(success);
        Assert.Null(coercedVariableValues);
        Assert.NotNull(error);

        new[] { error.WithException(null) }
            .ToList()
            .MatchInlineSnapshot(
                """
                "errors": [
                  {
                    "message": "The value `\"xyz\"` is not a valid value for the scalar type `Int`.",
                    "extensions": {
                      "variable": "abc"
                    }
                  }
                ]
                """);
    }

    [Fact]
    public void Error_When_List_Element_Type_Does_Not_Match()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new ListTypeNode(new NamedTypeNode("Int")),
                null,
                Array.Empty<DirectiveNode>())
        };

        using var variableValues = JsonDocument.Parse("""{"abc": [1, "xyz", 3]}""");

        // act
        var success = VariableCoercionHelper.TryCoerceVariableValues(
            new MockFeatureProvider(),
            schema,
            variableDefinitions,
            variableValues.RootElement,
            out var coercedVariableValues,
            out var error);

        // assert
        Assert.False(success);
        Assert.Null(coercedVariableValues);
        Assert.NotNull(error);

        new[] { error.WithException(null) }
            .ToList()
            .MatchInlineSnapshot(
                """
                "errors": [
                  {
                    "message": "The value `\"xyz\"` is not a valid value for the scalar type `Int`.",
                    "extensions": {
                      "variable": "abc[1]"
                    }
                  }
                ]
                """);
    }

    private sealed class MockFeatureProvider : IFeatureProvider
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
    }
}
