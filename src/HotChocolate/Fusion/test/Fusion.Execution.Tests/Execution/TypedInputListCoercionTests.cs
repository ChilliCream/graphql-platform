using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public class TypedInputListCoercionTests : FusionTestBase
{
    [Theory]
    [InlineData("Empty", "[Int!]!", "[]")]
    [InlineData("One", "[Int!]!", "[1]")]
    [InlineData("Three", "[Int!]!", "[2,3,4]")]
    [InlineData("Five", "[Int!]!", "[1,2,3,4,5]")]
    [InlineData("NullableItems", "[Int]", "[1,null,3]")]
    [InlineData("NestedLists", "[[Int]]", "[[1,null],[],[3]]")]
    [InlineData("Singleton", "[Int!]!", "1")]
    [InlineData("NullList", "[Int]", "null")]
    [InlineData("CustomScalarItems", "[JSON!]!", "[[1,2],{\"field\":[3]}]")]
    [InlineData("UntypedScalarArray", "JSON", "[1,2,3]")]
    [InlineData("Scalar", "Int!", "1")]
    public void TryCoerceVariableValues_Should_PreserveOwnedValues_When_InputIsValid(
        string scenario,
        string type,
        string input)
    {
        // arrange
        var schema = CreateSchema();
        var definition = CreateDefinition(type);
        VariableValue variable;

        // act
        using (var values = JsonDocument.Parse("{\"ids\":" + input + "}"))
        {
            variable = Coerce(schema, definition, values.RootElement);
        }

        // assert
        new
        {
            Type = variable.Type.ToString(),
            Literal = variable.Value.ToString(),
            EmptyItemsUseSharedArray = variable.Value is ListValueNode { Items.Count: 0 } list
                ? ReferenceEquals(list.Items, Array.Empty<IValueNode>())
                : (bool?)null
        }.MatchMarkdownSnapshot(scenario);
    }

    [Theory]
    [InlineData("InvalidSecondItem", "[Int!]!", "[1,\"wrong\",3]")]
    [InlineData("NullSecondItem", "[Int!]!", "[1,null,3]")]
    [InlineData("NestedNullItem", "[[Int!]!]!", "[[1],[2,null]]")]
    [InlineData("InvalidSingleton", "[Int!]!", "\"wrong\"")]
    public void TryCoerceVariableValues_Should_PreserveFirstError_When_ItemIsInvalid(
        string scenario,
        string type,
        string input)
    {
        // arrange
        var schema = CreateSchema();
        var definition = CreateDefinition(type);
        using var values = JsonDocument.Parse("{\"ids\":" + input + "}");

        // act
        var success = VariableCoercionHelper.TryCoerceVariableValues(
            new FeatureProvider(),
            schema,
            [definition],
            values.RootElement,
            ignoreAdditionalInputFields: false,
            out var coerced,
            out var error);

        // assert
        Snapshot.Create(scenario)
            .Add(new { Success = success, HasValues = coerced is not null })
            .Add(error is null ? Array.Empty<IError>() : [error.WithException(null)])
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void TryCoerceVariableValues_Should_KeepArraysIndependent_When_ValuesChange()
    {
        // arrange
        var schema = CreateSchema();
        var definition = CreateDefinition("[Int!]!");
        VariableValue first;
        VariableValue second;

        // act
        using (var values = JsonDocument.Parse("{\"ids\":[1,2,3]}"))
        {
            first = Coerce(schema, definition, values.RootElement);
        }

        using (var values = JsonDocument.Parse("{\"ids\":[4,5]}"))
        {
            second = Coerce(schema, definition, values.RootElement);
        }

        var firstItems = ((ListValueNode)first.Value).Items;
        var secondItems = ((ListValueNode)second.Value).Items;

        // assert
        new
        {
            SameArray = ReferenceEquals(firstItems, secondItems),
            First = first.Value.ToString(),
            Second = second.Value.ToString(),
            FirstCount = firstItems.Count,
            SecondCount = secondItems.Count
        }.MatchMarkdownSnapshot();
    }

    [Fact]
    public void TryCoerceVariableValues_Should_KeepArraysIndependent_When_RequestsAreConcurrent()
    {
        // arrange
        var schema = CreateSchema();
        var definition = CreateDefinition("[Int!]!");
        var results = new VariableValue[8];

        // act
        Parallel.For(0, results.Length, index =>
        {
            using var values = JsonDocument.Parse("{\"ids\":[" + index + "," + (index + 1) + "]}");
            results[index] = Coerce(schema, definition, values.RootElement);
        });

        // assert
        new
        {
            AllArraysAreIndependent = results.Select(value => ((ListValueNode)value.Value).Items)
                .Distinct(ReferenceEqualityComparer.Instance)
                .Count() == results.Length,
            Values = results.Select(value => value.Value.ToString()).ToArray()
        }.MatchMarkdownSnapshot();
    }

    private static FusionSchemaDefinition CreateSchema()
        => ComposeSchema(
            """
            type Query {
              field(ids: [Int!], json: JSON): String
            }

            scalar JSON
            """);

    private static VariableDefinitionNode CreateDefinition(string type)
        => ((OperationDefinitionNode)Utf8GraphQLParser.Parse(
            "query($ids: " + type + ") { __typename }").Definitions[0]).VariableDefinitions[0];

    private static VariableValue Coerce(
        FusionSchemaDefinition schema,
        VariableDefinitionNode definition,
        JsonElement values)
    {
        var success = VariableCoercionHelper.TryCoerceVariableValues(
            new FeatureProvider(),
            schema,
            [definition],
            values,
            ignoreAdditionalInputFields: false,
            out var coerced,
            out var error);
        Assert.True(success, error?.Message);
        return coerced!["ids"];
    }

    private sealed class FeatureProvider : IFeatureProvider
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
    }
}
