using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

public sealed class CostPlanTests
{
    private const string Directives =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(assumedSize: Int, slicingArguments: [String!], slicingArgumentDefaultValue: Float, sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION
        """ + "\n";

    [Fact]
    public void Evaluate_Should_PriceComplementaryConditions_When_VariableIsFalse()
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Side { costly: Int @cost(weight: "10") }
              type Query { left: Side right: Side }
              """,
            "query Example($x: Boolean!) { left { costly @include(if: $x) } right { costly @skip(if: $x) } }");

        // act
        var estimate = plan.Evaluate(Variables(("x", BooleanValueNode.False)));

        // assert
        Assert.Equal(new CostEstimate(12.0, 3.0, null), estimate);
    }

    [Fact]
    public void EvaluateStaticBound_Should_PriceComplementaryConditions_When_VariableIsUnknown()
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Side { costly: Int @cost(weight: "10") }
              type Query { left: Side right: Side }
              """,
            "query Example($x: Boolean!) { left { costly @include(if: $x) } right { costly @skip(if: $x) } }");

        // act
        var estimate = plan.EvaluateStaticBound();

        // assert
        Assert.Equal(new CostEstimate(12.0, 3.0, null), estimate);
    }

    [Theory]
    [InlineData("mutation { createBook(title: \"x\") { title } }", 4.0, 1.0)]
    [InlineData("mutation { a: createBook(title: \"x\") { title } b: createBook(title: \"y\") { title } }", 5.0, 2.0)]
    [InlineData("subscription { onBookAdded { title } }", 2.0, 1.0)]
    public void Evaluate_Should_PriceOperationRootOnce_When_OperationTypeVaries(
        string operation,
        double typeCost,
        double fieldCost)
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Query { book: Book }
              type Mutation @cost(weight: "3") { createBook(title: String): Book }
              type Subscription { onBookAdded: Book }
              type Book { title: String }
              """,
            operation);

        // act
        var estimate = plan.Evaluate(Variables());

        // assert
        Assert.Equal(new CostEstimate(fieldCost, typeCost, null), estimate);
    }

    [Fact]
    public void Evaluate_Should_ResolveInheritedSizedFieldVariable_When_PlanIsCached()
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Item { value: Int @cost(weight: "2") }
              type Container { items: [Item] }
              type Query {
                container(limit: Int!): Container
                  @listSize(assumedSize: 10, slicingArguments: ["limit"], sizedFields: ["items"])
              }
              """,
            "query($n: Int!) { container(limit: $n) { items { value } } }");

        // act
        var actual = plan.Evaluate(Variables(("n", new IntValueNode(3))));
        var bound = plan.EvaluateStaticBound();

        // assert
        Assert.Equal(new CostEstimate(8.0, 5.0, null), actual);
        Assert.Equal(new CostEstimate(22.0, 12.0, null), bound);
    }

    [Fact]
    public void Evaluate_Should_IncludeResponseSize_When_Requested()
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Item { value: Int }
              type Query { items(limit: Int!): [[Item]] @listSize(slicingArguments: ["limit"]) }
              """,
            "query($n: Int!) { items(limit: $n) { value } }",
            CostAnalyses.Cost | CostAnalyses.ResponseSize);

        // act
        var estimate = plan.Evaluate(Variables(("n", new IntValueNode(3))));

        // assert
        Assert.Equal(new CostEstimate(1.0, 4.0, 10.0), estimate);
    }

    [Fact]
    public void Evaluate_Should_AllocateNothing_When_PlanIsWarm()
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Item { value: Int @cost(weight: "2") }
              type Query { items(limit: Int!): [Item] @listSize(slicingArguments: ["limit"]) }
              """,
            "query($n: Int!) { items(limit: $n) { value } }");
        var variables = Variables(("n", new IntValueNode(3)));
        _ = plan.Evaluate(variables);

        // act
        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 1_000; i++)
        {
            _ = plan.Evaluate(variables);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // assert
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void Evaluate_Should_ReturnExactResults_When_PlanAndSnapshotAreSharedAndOptionsAreMutated()
    {
        // arrange
        var schema = SchemaParser.Parse(
            Directives
            + """
              type Item { value: Int }
              type Query { items(limit: Int): [Item] @listSize(slicingArguments: ["limit"]) }
              """);
        var options = new CostEngineOptions { DefaultListSize = 2.0 };
        var snapshot = CostSchemaSnapshot.Create(schema, options);
        var document = Utf8GraphQLParser.Parse("query($n: Int) { items(limit: $n) { value } }");
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var plan = CostPlanCompiler.Compile(
            snapshot,
            document,
            operation,
            CostAnalyses.Cost | CostAnalyses.ResponseSize);
        var estimates = new CostEstimate[64];
        var expected = new CostEstimate[64];
        options.DefaultListSize = 100.0;
        options.CaseBudget = 0;
        var returned = snapshot.Options;
        returned.DefaultListSize = 200.0;
        returned.CaseBudget = 0;

        // act
        Parallel.For(
            0,
            estimates.Length,
            index =>
            {
                if ((index & 1) == 0)
                {
                    var size = index % 8;
                    estimates[index] = plan.Evaluate(Variables(("n", new IntValueNode(size))));
                    expected[index] = new CostEstimate(1.0, size + 1.0, size + 1.0);
                }
                else
                {
                    estimates[index] = plan.Evaluate(Variables());
                    expected[index] = new CostEstimate(1.0, 3.0, 3.0);
                }
            });

        // assert
        Assert.Equal(expected, estimates);
        Assert.Equal(new CostEstimate(1.0, 3.0, 3.0), plan.EvaluateStaticBound());
    }

    private static CostPlan Compile(
        string source,
        string operationSource,
        CostAnalyses analyses = CostAnalyses.Cost)
    {
        var schema = SchemaParser.Parse(source);
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        var document = Utf8GraphQLParser.Parse(operationSource);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        return CostPlanCompiler.Compile(snapshot, document, operation, analyses);
    }

    private static ICostVariableValues Variables(params (string Name, IValueNode Value)[] values)
        => new TestVariableValues(values.ToDictionary(pair => pair.Name, pair => pair.Value));

    private sealed class TestVariableValues(
        IReadOnlyDictionary<string, IValueNode> values) : ICostVariableValues
    {
        public bool TryGetValue(string name, out IValueNode? value)
        {
            if (values.TryGetValue(name, out var found))
            {
                value = found;
                return true;
            }

            value = null;
            return false;
        }
    }
}
