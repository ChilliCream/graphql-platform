using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
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

    [Fact]
    public void TryCoerceVariableValues_Should_PreserveNestedErrorPaths()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              field(input: RootInput!): String
            }

            input RootInput {
              items: [ItemInput!]!
            }

            input ItemInput {
              id: Int!
            }
            """);
        var variableDefinition = CreateVariableDefinition(
            "input",
            new NonNullTypeNode(new NamedTypeNode("RootInput")));

        // act
        var errors = new[]
        {
            CoerceError(
                schema,
                variableDefinition,
                """{"input":{"items":[{"id":1},{"id":"wrong"}]}}"""),
            CoerceError(
                schema,
                variableDefinition,
                """{"input":{"items":[{"id":1},{}]}}"""),
            CoerceError(
                schema,
                variableDefinition,
                """{"input":{"items":[{"id":1,"unknown":"wrong"}]}}""")
        };

        // assert
        errors
            .Select(error => error.WithException(null))
            .ToList()
            .MatchInlineSnapshot(
                """
                "errors": [
                  {
                    "message": "The value `\"wrong\"` is not a valid value for the scalar type `Int`.",
                    "extensions": {
                      "variable": "input.items[1].id"
                    }
                  },
                  {
                    "message": "The field `unknown` is not defined on the input object type `ItemInput`.",
                    "extensions": {
                      "variable": "input.items[0]"
                    }
                  },
                  {
                    "message": "The required input field `id` is missing.",
                    "path": [
                      "input",
                      "items",
                      1,
                      "id"
                    ],
                    "extensions": {
                      "field": "ItemInput.id"
                    }
                  }
                ]
                """);
    }

    [Fact]
    public void TryCoerceVariableValues_Should_PreserveNestedOneOfErrors()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              field(input: RootInput!): String
            }

            input RootInput {
              choice: Choice!
            }

            input Choice @oneOf {
              left: String
              right: String
            }
            """);
        var variableDefinition = CreateVariableDefinition(
            "input",
            new NonNullTypeNode(new NamedTypeNode("RootInput")));

        // act
        var errors = new[]
        {
            CoerceError(schema, variableDefinition, """{"input":{"choice":{}}}"""),
            CoerceError(
                schema,
                variableDefinition,
                """{"input":{"choice":{"left":"a","right":"b"}}}"""),
            CoerceError(
                schema,
                variableDefinition,
                """{"input":{"choice":{"left":null,"right":"b"}}}"""),
            CoerceError(
                schema,
                variableDefinition,
                """{"input":{"choice":{"unknown":"a","right":"b"}}}"""),
            CoerceError(
                schema,
                variableDefinition,
                """{"input":{"choice":{"left":null}}}""")
        };

        // assert
        errors
            .Select(error => error.WithException(null))
            .ToList()
            .MatchInlineSnapshot(
                """
                "errors": [
                  {
                    "message": "The OneOf Input Object `Choice` requires that exactly one field is supplied and that field must not be `null`. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.",
                    "path": [
                      "input",
                      "choice"
                    ],
                    "extensions": {
                      "code": "HC0054"
                    }
                  },
                  {
                    "message": "More than one field of the OneOf Input Object `Choice` is set. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.",
                    "path": [
                      "input",
                      "choice"
                    ],
                    "extensions": {
                      "code": "HC0055"
                    }
                  },
                  {
                    "message": "More than one field of the OneOf Input Object `Choice` is set. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.",
                    "path": [
                      "input",
                      "choice"
                    ],
                    "extensions": {
                      "code": "HC0055"
                    }
                  },
                  {
                    "message": "More than one field of the OneOf Input Object `Choice` is set. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.",
                    "path": [
                      "input",
                      "choice"
                    ],
                    "extensions": {
                      "code": "HC0055"
                    }
                  },
                  {
                    "message": "`null` was set to the field `left` of the OneOf Input Object `Choice`. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.",
                    "path": [
                      "input",
                      "choice"
                    ],
                    "extensions": {
                      "code": "HC0056",
                      "coordinate": "Choice.left"
                    }
                  }
                ]
                """);
    }

    [Fact]
    public void TryCoerceVariableValues_Should_PreserveMaximumDepthBoundary()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var allowedDefinition = CreateVariableDefinition(
            "input",
            CreateNestedListType(64));
        var rejectedDefinition = CreateVariableDefinition(
            "input",
            CreateNestedListType(65));
        using var allowedValues = CreateNestedListValue(64);
        using var rejectedValues = CreateNestedListValue(65);

        // act
        var success = VariableCoercionHelper.TryCoerceVariableValues(
            new MockFeatureProvider(),
            schema,
            [allowedDefinition],
            allowedValues.RootElement,
            out var coercedVariableValues,
            out var error);
        var exception = Assert.Throws<InvalidOperationException>(
            () => VariableCoercionHelper.TryCoerceVariableValues(
                new MockFeatureProvider(),
                schema,
                [rejectedDefinition],
                rejectedValues.RootElement,
                out _,
                out _));

        // assert
        Assert.True(success, error?.Message);
        Assert.NotNull(coercedVariableValues);
        Assert.Null(error);
        Assert.Equal("Max allowed depth reached.", exception.Message);
    }

    [Fact]
    public void TryCoerceVariableValues_Should_PreserveNumericLexemesAndFormats()
    {
        // arrange
        var schema = ComposeSchema(
            """
            scalar Any

            type Query {
              field(value: Any, integer: Int, floatingPoint: Float): String
            }
            """);
        var variableDefinitions = new List<VariableDefinitionNode>
        {
            CreateVariableDefinition("one", new NamedTypeNode("Int")),
            CreateVariableDefinition("negativeZero", new NamedTypeNode("Int")),
            CreateVariableDefinition("fixedPoint", new NamedTypeNode("Float")),
            CreateVariableDefinition("lowerExponent", new NamedTypeNode("Float")),
            CreateVariableDefinition("upperExponent", new NamedTypeNode("Float")),
            CreateVariableDefinition("large", new NamedTypeNode("Any"))
        };
        using var variableValues = JsonDocument.Parse(
            """
            {
              "one": 1,
              "negativeZero": -0,
              "fixedPoint": 1.0,
              "lowerExponent": 1e3,
              "upperExponent": 1E-3,
              "large": 123456789012345678901234567890
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

        var one = Assert.IsType<IntValueNode>(coercedVariableValues["one"].Value);
        var negativeZero = Assert.IsType<IntValueNode>(coercedVariableValues["negativeZero"].Value);
        var fixedPoint = Assert.IsType<FloatValueNode>(coercedVariableValues["fixedPoint"].Value);
        var lowerExponent = Assert.IsType<FloatValueNode>(coercedVariableValues["lowerExponent"].Value);
        var upperExponent = Assert.IsType<FloatValueNode>(coercedVariableValues["upperExponent"].Value);
        var large = Assert.IsType<IntValueNode>(coercedVariableValues["large"].Value);

        Assert.Equal("1", one.Value);
        Assert.Equal("-0", negativeZero.Value);
        Assert.Equal("1.0", fixedPoint.Value);
        Assert.Equal(FloatFormat.FixedPoint, fixedPoint.Format);
        Assert.Equal("1e3", lowerExponent.Value);
        Assert.Equal(FloatFormat.Exponential, lowerExponent.Format);
        Assert.Equal("1E-3", upperExponent.Value);
        Assert.Equal(FloatFormat.Exponential, upperExponent.Format);
        Assert.Equal("123456789012345678901234567890", large.Value);
    }

    private static IError CoerceError(
        FusionSchemaDefinition schema,
        VariableDefinitionNode variableDefinition,
        string variableValues)
    {
        using var document = JsonDocument.Parse(variableValues);
        var success = VariableCoercionHelper.TryCoerceVariableValues(
            new MockFeatureProvider(),
            schema,
            [variableDefinition],
            document.RootElement,
            out var coercedVariableValues,
            out var error);

        Assert.False(success);
        Assert.Null(coercedVariableValues);
        return Assert.IsAssignableFrom<IError>(error);
    }

    private static VariableDefinitionNode CreateVariableDefinition(
        string name,
        ITypeNode type)
        => new(
            null,
            new VariableNode(name),
            description: null,
            type,
            defaultValue: null,
            Array.Empty<DirectiveNode>());

    private static ITypeNode CreateNestedListType(int depth)
    {
        ITypeNode type = new NamedTypeNode("Int");

        for (var i = 0; i < depth; i++)
        {
            type = new ListTypeNode(type);
        }

        return type;
    }

    private static JsonDocument CreateNestedListValue(int depth)
        => JsonDocument.Parse(
            "{\"input\":"
            + new string('[', depth)
            + "1"
            + new string(']', depth)
            + "}",
            new JsonDocumentOptions { MaxDepth = depth + 2 });

    private sealed class MockFeatureProvider : IFeatureProvider
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
    }
}
