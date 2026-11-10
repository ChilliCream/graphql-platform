using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

public class InputCostTests
{
    [Fact]
    public void Compute_Should_MaterializeNestedDefaults_When_InputObjectIsSupplied()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { search(filter: Filter): String }
            input Filter { nested: Nested = {} @cost(weight: "-1") }
            input Nested { limit: Int = 2 @cost(weight: "4") }
            """);
        var argument = schemaIndex.GetFieldArguments("Query", "search")[0];

        // act
        var cost = InputCost.Compute(schemaIndex, argument, new ObjectValueNode(), new FakeCostVariableValues());

        // assert
        Assert.Equal(4.0, cost);
    }

    [Fact]
    public void Compute_Should_PayListContainerWeightOnce_When_ListContainsObjects()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { search(filters: [Filter] @cost(weight: "5")): String }
            input Filter { term: String @cost(weight: "2") }
            """);
        var argument = schemaIndex.GetFieldArguments("Query", "search")[0];
        var value = new ListValueNode(
            new ObjectValueNode(new ObjectFieldNode("term", "a")),
            new ObjectValueNode(new ObjectFieldNode("term", "b")));

        // act
        var cost = InputCost.Compute(schemaIndex, argument, value, new FakeCostVariableValues());

        // assert
        Assert.Equal(9.0, cost);
    }

    [Fact]
    public void Compute_Should_CutCycles_When_VariableUsesStaticShape()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { search(filter: Recursive): String }
            input Recursive {
                child: Recursive @cost(weight: "2")
                term: String @cost(weight: "3")
            }
            """);
        var argument = schemaIndex.GetFieldArguments("Query", "search")[0];

        // act
        var cost = InputCost.Compute(schemaIndex, argument, new VariableNode("filter"), variableValues: null);

        // assert
        Assert.Equal(6.0, cost);
    }

    [Fact]
    public void Compute_Should_UseSchemaDefaultForUndefinedVariable_AndNotForNull()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { search(filter: Filter = { nested: {} }): String }
            input Filter { nested: Nested @cost(weight: "2") }
            input Nested { limit: Int = 2 @cost(weight: "4") }
            """);
        var argument = schemaIndex.GetFieldArguments("Query", "search")[0];
        var undefinedValues = new FakeCostVariableValues();
        var nullValues = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["filter"] = NullValueNode.Default
        });

        // act
        var undefinedCost = InputCost.Compute(schemaIndex, argument, new VariableNode("filter"), undefinedValues);
        var nullCost = InputCost.Compute(schemaIndex, argument, new VariableNode("filter"), nullValues);

        // assert
        Assert.Equal(7.0, undefinedCost);
        Assert.Equal(1.0, nullCost);
    }

    [Fact]
    public void Compute_Should_ResolveDefinedObjectVariableInsideList()
    {
        // arrange
        var (schemaIndex, argument) = BuildListFilterArgument();
        var variables = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["filter"] = new ObjectValueNode(new ObjectFieldNode("term", "a"))
        });
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(schemaIndex, argument, value, variables);

        // assert
        Assert.Equal(7.0, cost);
    }

    [Fact]
    public void Compute_Should_UseOperationDefaultValueRepresentedByVariableValuesInsideList()
    {
        // arrange
        var (schemaIndex, argument) = BuildListFilterArgument();
        var operationDefaultValues = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["filter"] = new ObjectValueNode(new ObjectFieldNode("term", "default"))
        });
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(schemaIndex, argument, value, operationDefaultValues);

        // assert
        Assert.Equal(7.0, cost);
    }

    [Fact]
    public void Compute_Should_UseStaticShapeForVariableInsideList()
    {
        // arrange
        var (schemaIndex, argument) = BuildListFilterArgument();
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(schemaIndex, argument, value, variableValues: null);

        // assert
        Assert.Equal(7.0, cost);
    }

    [Fact]
    public void Compute_Should_OmitUndefinedVariableInsideList()
    {
        // arrange
        var (schemaIndex, argument) = BuildListFilterArgument();
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(schemaIndex, argument, value, new FakeCostVariableValues());

        // assert
        Assert.Equal(5.0, cost);
    }

    [Fact]
    public void Compute_Should_OmitNullVariableInsideList()
    {
        // arrange
        var (schemaIndex, argument) = BuildListFilterArgument();
        var variables = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["filter"] = NullValueNode.Default
        });
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(schemaIndex, argument, value, variables);

        // assert
        Assert.Equal(5.0, cost);
    }

    [Fact]
    public void Compute_Should_ResolveVariableInsideNestedLists()
    {
        // arrange
        var (schemaIndex, argument) = BuildListFilterArgument();
        var variables = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["filter"] = new ObjectValueNode(new ObjectFieldNode("term", "a"))
        });
        var value = new ListValueNode(new ListValueNode(new VariableNode("filter")));

        // act
        var cost = InputCost.Compute(schemaIndex, argument, value, variables);

        // assert
        Assert.Equal(7.0, cost);
    }

    [Fact]
    public void Compute_Should_PayListContainerWeightOnceForVariableItems()
    {
        // arrange
        var (schemaIndex, argument) = BuildListFilterArgument();
        var variables = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["first"] = new ObjectValueNode(new ObjectFieldNode("term", "a")),
            ["second"] = new ObjectValueNode(new ObjectFieldNode("term", "b"))
        });
        var value = new ListValueNode(new VariableNode("first"), new VariableNode("second"));

        // act
        var cost = InputCost.Compute(schemaIndex, argument, value, variables);

        // assert
        Assert.Equal(9.0, cost);
    }

    [Fact]
    public void Compute_Should_CutCyclesForStaticShapeInsideList()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { search(filters: [Recursive] @cost(weight: "5")): String }
            input Recursive { child: Recursive @cost(weight: "2") }
            """);
        var argument = schemaIndex.GetFieldArguments("Query", "search")[0];
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(schemaIndex, argument, value, variableValues: null);

        // assert
        Assert.Equal(7.0, cost);
    }

    [Fact]
    public void Compute_Should_CountSharedNestedTypeOnce_When_VariableUsesStaticShape()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { search(filter: Filter): String }
            input Filter {
                a: Nested @cost(weight: "1")
                b: Nested @cost(weight: "1")
            }
            input Nested { term: String @cost(weight: "3") }
            """);
        var argument = schemaIndex.GetFieldArguments("Query", "search")[0];

        // act
        var cost = InputCost.Compute(schemaIndex, argument, new VariableNode("filter"), variableValues: null);

        // assert
        Assert.Equal(6.0, cost);
    }

    [Fact]
    public void Compute_Should_CountEveryReachableFieldOnce_When_InputTypesAreMutuallyRecursive()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { search(filter: Filter): String }
            input Filter {
                and: [Filter] @cost(weight: "1")
                or: [Filter] @cost(weight: "1")
                item: ItemFilter @cost(weight: "2")
            }
            input ItemFilter {
                filter: Filter @cost(weight: "1")
                name: String @cost(weight: "3")
            }
            """);
        var argument = schemaIndex.GetFieldArguments("Query", "search")[0];

        // act
        var cost = InputCost.Compute(schemaIndex, argument, new VariableNode("filter"), variableValues: null);

        // assert
        Assert.Equal(9.0, cost);
    }

    [Fact]
    public void Compute_Should_StayLinear_When_ManyInputTypesReferenceEachOther()
    {
        // arrange
        // 24 input types that each reference all 24 (themselves included). Expanding every path through
        // this graph, which the static shape used to do, takes factorial time and never finishes here.
        const int typeCount = 24;
        var sdl = new System.Text.StringBuilder();
        sdl.AppendLine("type Query { search(filter: Filter0): String }");

        for (var i = 0; i < typeCount; i++)
        {
            sdl.AppendLine($"input Filter{i} {{");

            for (var j = 0; j < typeCount; j++)
            {
                sdl.AppendLine($"  field{j}: Filter{j} @cost(weight: \"1\")");
            }

            sdl.AppendLine("}");
        }

        var schemaIndex = BuildSchemaIndex(sdl.ToString());
        var argument = schemaIndex.GetFieldArguments("Query", "search")[0];

        // act
        var cost = InputCost.Compute(schemaIndex, argument, new VariableNode("filter"), variableValues: null);

        // assert
        Assert.Equal(1.0 + typeCount * typeCount, cost);
    }

    private static (CostSchemaIndex SchemaIndex, InputValueMetadata Argument) BuildListFilterArgument()
    {
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { search(filters: [Filter] @cost(weight: "5")): String }
            input Filter { term: String @cost(weight: "2") }
            """);

        return (schemaIndex, schemaIndex.GetFieldArguments("Query", "search")[0]);
    }

    private static CostSchemaIndex BuildSchemaIndex(string sdl)
        => CostSchemaIndex.Create(SchemaParser.Parse(sdl), new CostSchemaIndexOptions());

    private sealed class FakeCostVariableValues(Dictionary<string, IValueNode?>? values = null) : ICostVariableValues
    {
        private readonly Dictionary<string, IValueNode?> _values = values ?? [];

        public bool TryGetValue(string name, out IValueNode? value)
            => _values.TryGetValue(name, out value);
    }
}
