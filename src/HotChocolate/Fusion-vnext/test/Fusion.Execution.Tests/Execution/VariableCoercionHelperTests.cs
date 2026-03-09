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

    private sealed class MockFeatureProvider : IFeatureProvider
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
    }
}
