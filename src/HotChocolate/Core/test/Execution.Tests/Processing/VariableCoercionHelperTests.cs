using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.StarWars.Models;
using HotChocolate.StarWars.Types;
using HotChocolate.Types;
using Moq;

namespace HotChocolate.Execution.Processing;

public class VariableCoercionHelperTests
{
    [Fact]
    public void VariableCoercionHelper_Schema_Is_Null()
    {
        // arrange
        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("String"),
                new StringValueNode("def"),
                Array.Empty<DirectiveNode>())
        };

        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        void Action() => helper.CoerceVariableValues(
            null!, variableDefinitions, default, coercedValues, featureProvider.Object);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void VariableCoercionHelper_VariableDefinitions_Is_Null()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();
        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        void Action()
            => helper.CoerceVariableValues(schema, null!, default, coercedValues, featureProvider.Object);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void VariableCoercionHelper_CoercedValues_Is_Null()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new VariableDefinitionNode(
                null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("String"),
                new StringValueNode("def"),
                Array.Empty<DirectiveNode>())
        };

        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        void Action() => helper.CoerceVariableValues(
            schema, variableDefinitions, default, null!, featureProvider.Object);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Coerce_Nullable_String_Variable_With_Default_Where_Value_Is_Not_Provided()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new VariableDefinitionNode(
                null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("String"),
                new StringValueNode("def"),
                Array.Empty<DirectiveNode>())
        };

        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema, variableDefinitions, default, coercedValues, featureProvider.Object);

        // assert
        Assert.Collection(coercedValues,
            t =>
            {
                Assert.Equal("abc", t.Key);
                Assert.Equal("String", Assert.IsType<StringType>(t.Value.Type).Name);
                Assert.Equal("def", t.Value.RuntimeValue);
                Assert.Equal("def", Assert.IsType<StringValueNode>(t.Value.ValueLiteral).Value);
            });
    }

    [Fact]
    public void Coerce_Nullable_String_Variable_Where_Value_Is_Not_Provided()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

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

        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema, variableDefinitions, default, coercedValues, featureProvider.Object);

        // assert
        Assert.Empty(coercedValues);
    }

    [Fact]
    public void Coerce_Nullable_String_Variable_With_Default_Where_Value_Is_Provided()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new VariableDefinitionNode(
                null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("String"),
                new StringValueNode("def"),
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse("""{"abc": "xyz"}""");
        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        Assert.Collection(coercedValues,
            t =>
            {
                Assert.Equal("abc", t.Key);
                Assert.Equal("String", Assert.IsType<StringType>(t.Value.Type).Name);
                Assert.Equal("xyz", t.Value.RuntimeValue);
                Assert.Equal("xyz", Assert.IsType<StringValueNode>(t.Value.ValueLiteral).Value);
            });
    }

    [Fact]
    public void Coerce_Nullable_String_Variable_With_Default_Where_Plain_Value_Is_Provided()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new VariableDefinitionNode(
                null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("String"),
                new StringValueNode("def"),
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse("""{"abc": "xyz"}""");
        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        Assert.Collection(coercedValues,
            t =>
            {
                Assert.Equal("abc", t.Key);
                Assert.Equal("String", Assert.IsType<StringType>(t.Value.Type).Name);
                Assert.Equal("xyz", t.Value.RuntimeValue);
                t.Value.ValueLiteral.MatchInlineSnapshot("\"xyz\"");
            });
    }

    [Fact]
    public void Coerce_Nullable_String_Variable_With_Default_Where_Null_Is_Provided()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new VariableDefinitionNode(
                null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("String"),
                new StringValueNode("def"),
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse("""{"abc": null}""");
        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        Assert.Collection(coercedValues,
            t =>
            {
                Assert.Equal("abc", t.Key);
                Assert.Equal("String", Assert.IsType<StringType>(t.Value.Type).Name);
                Assert.Null(t.Value.RuntimeValue);
                Assert.IsType<NullValueNode>(t.Value.ValueLiteral);
            });
    }

    [Fact]
    public void Coerce_Nullable_ReviewInput_Variable_With_Object()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new VariableDefinitionNode(
                null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("ReviewInput"),
                new StringValueNode("def"),
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse("""{"abc": {"stars": 5}}""");
        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        Assert.Collection(coercedValues,
            t =>
            {
                Assert.Equal("abc", t.Key);
                Assert.Equal("ReviewInput", Assert.IsType<ReviewInputType>(t.Value.Type).Name);
                Assert.Equal(5, Assert.IsType<Review>(t.Value.RuntimeValue).Stars);
                Assert.IsType<ObjectValueNode>(t.Value.ValueLiteral);
            });
    }

    [Fact]
    public void Error_When_Value_Is_Null_On_Non_Null_Variable()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new VariableDefinitionNode(
                null,
                new VariableNode("abc"),
                description: null,
                new NonNullTypeNode(new NamedTypeNode("String")),
                new StringValueNode("def"),
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse("""{"abc": null}""");
        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        void Action() => helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        Assert.Throws<LeafCoercionException>(Action)
            .Errors.Select(t => t.WithException(null))
            .ToList()
            .MatchInlineSnapshot(
                """
                [
                  {
                    "Message": "Cannot accept null for non-nullable input.",
                    "Code": null,
                    "Path": null,
                    "Locations": null,
                    "Extensions": null
                  }
                ]
                """);
    }

    [Fact]
    public void Error_When_Value_Type_Does_Not_Match_Variable_Type()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new VariableDefinitionNode(
                null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("String"),
                new StringValueNode("def"),
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse("""{"abc": 1}""");
        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        void Action() => helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        Assert.Throws<LeafCoercionException>(Action)
            .Errors.Select(t => t.WithException(null))
            .ToList()
            .MatchInlineSnapshot(
                """
                [
                  {
                    "Message": "The value `1` is not compatible with the type `String`.",
                    "Code": null,
                    "Path": null,
                    "Locations": null,
                    "Extensions": {
                      "variable": "abc"
                    }
                  }
                ]
                """);
    }

    [Fact]
    public void Variable_Type_Is_Not_An_Input_Type()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("Human"),
                new StringValueNode("def"),
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse("""{"abc": 1}""");
        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        void Action() => helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        Assert.Throws<GraphQLException>(Action).Errors.MatchInlineSnapshot(
            """
            [
              {
                "Message": "Variable `abc` has an invalid type `Human`.",
                "Code": null,
                "Path": null,
                "Locations": null,
                "Extensions": null
              }
            ]
            """);
    }

    [Fact]
    public void Error_When_Input_Field_Has_Different_Properties_Than_Defined()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("ReviewInput"),
                new StringValueNode("def"),
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse("""{"abc": {"abc": "def"}}""");
        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        void Action() => helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        Assert.Throws<LeafCoercionException>(Action)
            .Errors.Select(t => t.WithException(null))
            .ToList()
            .MatchInlineSnapshot(
                """
                [
                  {
                    "Message": "`stars` is a required field of `ReviewInput`.",
                    "Code": null,
                    "Path": null,
                    "Locations": null,
                    "Extensions": {
                      "field": "stars",
                      "type": "ReviewInput",
                      "variable": "abc"
                    }
                  }
                ]
                """);
    }

    [Fact]
    public void StringValues_Representing_EnumValues_In_Lists_ShouldBe_Rewritten()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                    type Query {
                        test(list: [FooInput]): String
                    }

                    input FooInput {
                        enum: TestEnum
                    }

                    enum TestEnum {
                        Foo
                        Bar
                    }")
            .Use(_ => _ => default)
            .Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new ListTypeNode(new NamedTypeNode("FooInput")),
                null,
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse(
            """
            {
              "abc": [
                { "enum": "Foo" },
                { "enum": "Bar" }
              ]
            }
            """);

        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        var entry = Assert.Single(coercedValues);
        Assert.Equal("abc", entry.Key);
        entry.Value.ValueLiteral.MatchInlineSnapshot(
            """
            [
              {
                enum: Foo
              },
              {
                enum: Bar
              }
            ]
            """);
    }

    [Fact]
    public void StringValues_Representing_NonNullEnumValues_In_Lists_ShouldBe_Rewritten()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                    type Query {
                        test(list: [FooInput]): String
                    }

                    input FooInput {
                        enum: TestEnum!
                    }

                    enum TestEnum {
                        Foo
                        Bar
                    }")
            .Use(_ => _ => default)
            .Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new ListTypeNode(new NamedTypeNode("FooInput")),
                null,
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse(
            """
            {
              "abc": [
                { "enum": "Foo" },
                { "enum": "Bar" }
              ]
            }
            """);

        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        var entry = Assert.Single(coercedValues);
        Assert.Equal("abc", entry.Key);
        entry.Value.ValueLiteral.MatchInlineSnapshot(
            """
            [
              {
                enum: Foo
              },
              {
                enum: Bar
              }
            ]
            """);
    }

    [Fact]
    public void StringValues_Representing_EnumValues_In_Objects_ShouldBe_Rewritten()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                    type Query {
                        test(list: FooInput): String
                    }

                    input FooInput {
                        enum: TestEnum
                        enum2: TestEnum
                    }

                    enum TestEnum {
                        Foo
                        Bar
                    }")
            .Use(_ => _ => default)
            .Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("FooInput"),
                null,
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse(
            """
            {
              "abc": { "enum": "Foo", "enum2": "Bar" }
            }
            """);

        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        var entry = Assert.Single(coercedValues);
        Assert.Equal("abc", entry.Key);
        entry.Value.ValueLiteral.MatchInlineSnapshot(
            """
            {
              enum: Foo,
              enum2: Bar
            }
            """);
    }

    [Fact]
    public void StringValues_Representing_NonNullEnumValues_In_Objects_ShouldBe_Rewritten()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                    type Query {
                        test(list: FooInput): String
                    }

                    input FooInput {
                        enum: TestEnum!
                        enum2: TestEnum!
                    }

                    enum TestEnum {
                        Foo
                        Bar
                    }")
            .Use(_ => _ => default)
            .Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("FooInput"),
                null,
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse(
            """
            {
              "abc": { "enum": "Foo", "enum2": "Bar" }
            }
            """);

        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        var entry = Assert.Single(coercedValues);
        Assert.Equal("abc", entry.Key);
        entry.Value.ValueLiteral.MatchInlineSnapshot(
            """
            {
              enum: Foo,
              enum2: Bar
            }
            """);
    }

    [Fact]
    public void If_Second_Item_In_Object_Is_Rewritten_The_Previous_Values_Are_Correctly_Copied()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                    type Query {
                        test(list: FooInput): String
                    }

                    input FooInput {
                        value_a: String
                        value_b: TestEnum
                    }

                    enum TestEnum {
                        Foo
                        Bar
                    }")
            .Use(_ => _ => default)
            .Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("FooInput"),
                null,
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse(
            """
            {
              "abc": { "value_a": "Foo", "value_b": "Bar" }
            }
            """);

        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();
        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema, variableDefinitions, variableValues.RootElement, coercedValues, featureProvider.Object);

        // assert
        var entry = Assert.Single(coercedValues);
        Assert.Equal("abc", entry.Key);
        entry.Value.ValueLiteral.MatchInlineSnapshot(
            """
            {
              value_a: "Foo",
              value_b: Bar
            }
            """);
    }

    [Fact]
    public void If_Second_Item_In_List_Is_Rewritten_The_Previous_Values_Are_Correctly_Copied()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                    type Query {
                        test(list: [FooInput]): String
                    }

                    input FooInput {
                        value_a: String
                        value_b: TestEnum
                    }

                    enum TestEnum {
                        Foo
                        Bar
                    }")
            .Use(_ => _ => default)
            .Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new ListTypeNode(new NamedTypeNode("FooInput")),
                null,
                Array.Empty<DirectiveNode>())
        };

        var variableValues = JsonDocument.Parse(
            """
            {
              "abc": [
                {
                  "value_a": "Foo"
                },
                {
                  "value_b": "Bar"
                }
              ]
            }
            """);

        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();

        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema,
            variableDefinitions,
            variableValues.RootElement,
            coercedValues,
            featureProvider.Object);

        // assert
        var entry = Assert.Single(coercedValues);
        Assert.Equal("abc", entry.Key);
        entry.Value.ValueLiteral.MatchInlineSnapshot(
            """
            [
              {
                value_a: "Foo"
              },
              {
                value_b: Bar
              }
            ]
            """);
    }

    [Fact]
    public void Variable_Is_Nullable_And_Not_Set()
    {
        // arrange
        var schema = SchemaBuilder.New().AddStarWarsTypes().Create();

        var variableDefinitions = new List<VariableDefinitionNode>
        {
            new(null,
                new VariableNode("abc"),
                description: null,
                new NamedTypeNode("String"),
                null,
                Array.Empty<DirectiveNode>())
        };

        var coercedValues = new Dictionary<string, VariableValue>();
        var featureProvider = new Mock<IFeatureProvider>();

        var helper = new VariableCoercionHelper(new());

        // act
        helper.CoerceVariableValues(
            schema,
            variableDefinitions,
            default,
            coercedValues,
            featureProvider.Object);

        // assert
        Assert.Empty(coercedValues);
    }
}
