using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

public class InputCostTests
{
    [Fact]
    public void Compute_Should_MaterializeNestedDefaults_When_InputObjectIsSupplied()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { search(filter: Filter): String }
            input Filter { nested: Nested = {} @cost(weight: "-1") }
            input Nested { limit: Int = 2 @cost(weight: "4") }
            """);
        var argument = snapshot.GetFieldArguments("Query", "search")[0];

        // act
        var cost = InputCost.Compute(snapshot, argument, new ObjectValueNode(), new FakeCostVariableValues());

        // assert
        Assert.Equal(4.0, cost);
    }

    [Fact]
    public void Compute_Should_PayListContainerWeightOnce_When_ListContainsObjects()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { search(filters: [Filter] @cost(weight: "5")): String }
            input Filter { term: String @cost(weight: "2") }
            """);
        var argument = snapshot.GetFieldArguments("Query", "search")[0];
        var value = new ListValueNode(
            new ObjectValueNode(new ObjectFieldNode("term", "a")),
            new ObjectValueNode(new ObjectFieldNode("term", "b")));

        // act
        var cost = InputCost.Compute(snapshot, argument, value, new FakeCostVariableValues());

        // assert
        Assert.Equal(9.0, cost);
    }

    [Fact]
    public void Compute_Should_CutCycles_When_VariableUsesStaticShape()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { search(filter: Recursive): String }
            input Recursive {
                child: Recursive @cost(weight: "2")
                term: String @cost(weight: "3")
            }
            """);
        var argument = snapshot.GetFieldArguments("Query", "search")[0];

        // act
        var cost = InputCost.Compute(snapshot, argument, new VariableNode("filter"), variableValues: null);

        // assert
        Assert.Equal(6.0, cost);
    }

    [Fact]
    public void Compute_Should_UseSchemaDefaultForUndefinedVariable_AndNotForNull()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { search(filter: Filter = { nested: {} }): String }
            input Filter { nested: Nested @cost(weight: "2") }
            input Nested { limit: Int = 2 @cost(weight: "4") }
            """);
        var argument = snapshot.GetFieldArguments("Query", "search")[0];
        var undefinedValues = new FakeCostVariableValues();
        var nullValues = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["filter"] = NullValueNode.Default
        });

        // act
        var undefinedCost = InputCost.Compute(snapshot, argument, new VariableNode("filter"), undefinedValues);
        var nullCost = InputCost.Compute(snapshot, argument, new VariableNode("filter"), nullValues);

        // assert
        Assert.Equal(7.0, undefinedCost);
        Assert.Equal(1.0, nullCost);
    }

    [Fact]
    public void Compute_Should_ResolveDefinedObjectVariableInsideList()
    {
        // arrange
        var (snapshot, argument) = BuildListFilterArgument();
        var variables = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["filter"] = new ObjectValueNode(new ObjectFieldNode("term", "a"))
        });
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(snapshot, argument, value, variables);

        // assert
        Assert.Equal(7.0, cost);
    }

    [Fact]
    public void Compute_Should_UseOperationDefaultValueRepresentedByVariableValuesInsideList()
    {
        // arrange
        var (snapshot, argument) = BuildListFilterArgument();
        var operationDefaultValues = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["filter"] = new ObjectValueNode(new ObjectFieldNode("term", "default"))
        });
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(snapshot, argument, value, operationDefaultValues);

        // assert
        Assert.Equal(7.0, cost);
    }

    [Fact]
    public void Compute_Should_UseStaticShapeForVariableInsideList()
    {
        // arrange
        var (snapshot, argument) = BuildListFilterArgument();
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(snapshot, argument, value, variableValues: null);

        // assert
        Assert.Equal(7.0, cost);
    }

    [Fact]
    public void Compute_Should_OmitUndefinedVariableInsideList()
    {
        // arrange
        var (snapshot, argument) = BuildListFilterArgument();
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(snapshot, argument, value, new FakeCostVariableValues());

        // assert
        Assert.Equal(5.0, cost);
    }

    [Fact]
    public void Compute_Should_OmitNullVariableInsideList()
    {
        // arrange
        var (snapshot, argument) = BuildListFilterArgument();
        var variables = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["filter"] = NullValueNode.Default
        });
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(snapshot, argument, value, variables);

        // assert
        Assert.Equal(5.0, cost);
    }

    [Fact]
    public void Compute_Should_ResolveVariableInsideNestedLists()
    {
        // arrange
        var (snapshot, argument) = BuildListFilterArgument();
        var variables = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["filter"] = new ObjectValueNode(new ObjectFieldNode("term", "a"))
        });
        var value = new ListValueNode(new ListValueNode(new VariableNode("filter")));

        // act
        var cost = InputCost.Compute(snapshot, argument, value, variables);

        // assert
        Assert.Equal(7.0, cost);
    }

    [Fact]
    public void Compute_Should_PayListContainerWeightOnceForVariableItems()
    {
        // arrange
        var (snapshot, argument) = BuildListFilterArgument();
        var variables = new FakeCostVariableValues(new Dictionary<string, IValueNode?>
        {
            ["first"] = new ObjectValueNode(new ObjectFieldNode("term", "a")),
            ["second"] = new ObjectValueNode(new ObjectFieldNode("term", "b"))
        });
        var value = new ListValueNode(new VariableNode("first"), new VariableNode("second"));

        // act
        var cost = InputCost.Compute(snapshot, argument, value, variables);

        // assert
        Assert.Equal(9.0, cost);
    }

    [Fact]
    public void Compute_Should_CutCyclesForStaticShapeInsideList()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { search(filters: [Recursive] @cost(weight: "5")): String }
            input Recursive { child: Recursive @cost(weight: "2") }
            """);
        var argument = snapshot.GetFieldArguments("Query", "search")[0];
        var value = new ListValueNode(new VariableNode("filter"));

        // act
        var cost = InputCost.Compute(snapshot, argument, value, variableValues: null);

        // assert
        Assert.Equal(7.0, cost);
    }

    private static (CostSchemaSnapshot Snapshot, InputValueMetadata Argument) BuildListFilterArgument()
    {
        var snapshot = BuildSnapshot(
            """
            type Query { search(filters: [Filter] @cost(weight: "5")): String }
            input Filter { term: String @cost(weight: "2") }
            """);

        return (snapshot, snapshot.GetFieldArguments("Query", "search")[0]);
    }

    private static CostSchemaSnapshot BuildSnapshot(string sdl)
        => CostSchemaSnapshot.Create(SchemaParser.Parse(sdl), new CostEngineOptions());

    private sealed class FakeCostVariableValues(Dictionary<string, IValueNode?>? values = null) : ICostVariableValues
    {
        private readonly Dictionary<string, IValueNode?> _values = values ?? [];

        public bool TryGetValue(string name, out IValueNode? value)
            => _values.TryGetValue(name, out value);
    }
}
