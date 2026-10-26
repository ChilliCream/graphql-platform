using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

public sealed class DeferredBranchReuseTests
{
    private const string Directives =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(assumedSize: Int, slicingArguments: [String!], sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION
        """ + "\n";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(510)]
    public void Evaluate_Should_PreserveEveryAssignment_When_RuntimePlanReusesDeferredBranches(int budget)
    {
        // arrange
        var (schema, operation) = CreateIndependentOperation(9);
        var plan = Compile(schema, operation, budget, CaseBudgetExceededBehavior.EvaluatePerRequest);
        var expected = Enumerable.Range(0, 512)
            .Select(mask => new CostEstimate(ExpectedFieldCost(mask, 9), 1, null))
            .ToArray();

        // act
        var actual = Enumerable.Range(0, 512)
            .Select(mask => plan.Evaluate(new VariableValues(CreateVariables(mask, 9))))
            .ToArray();

        // assert
        Assert.True(plan.HitCaseBudget);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Evaluate_Should_IsolateConcurrentCursors_When_OneCompiledPlanIsReused()
    {
        // arrange
        var (schema, operation) = CreateIndependentOperation(9);
        var plan = Compile(schema, operation, 510, CaseBudgetExceededBehavior.EvaluatePerRequest);
        var expectedBound = plan.EvaluateAssumedBound();
        var expected = Enumerable.Range(0, 64)
            .Select(i => new CostEstimate(ExpectedFieldCost((i * 17) % 512, 9), 1, null))
            .ToArray();
        var actual = new CostEstimate[expected.Length];

        // act
        Parallel.For(0, actual.Length, i =>
        {
            var variables = new VariableValues(CreateVariables((i * 17) % 512, 9));
            _ = plan.Evaluate(variables);
            actual[i] = plan.Evaluate(variables);
        });
        var actualBound = plan.EvaluateAssumedBound();

        // assert
        Assert.Equal(expected, actual);
        Assert.Equal(expectedBound, actualBound);
        Assert.Equal(new CostEstimate(45, 1, null), actualBound);
    }

    [Theory]
    [InlineData(CaseBudgetExceededBehavior.EvaluatePerRequest, 0)]
    [InlineData(CaseBudgetExceededBehavior.Overestimate, 45)]
    public void Evaluate_Should_PreserveStaticAssumptions_When_RuntimeEvaluationsInterleave(
        CaseBudgetExceededBehavior behavior,
        double expectedFalseCost)
    {
        // arrange
        var (schema, operation) = CreateIndependentOperation(9);
        var plan = Compile(schema, operation, 0, behavior);
        var before = plan.EvaluateAssumedBound();

        // act
        var falseValue = plan.Evaluate(new VariableValues(CreateVariables(0, 9)));
        var trueValue = plan.Evaluate(new VariableValues(CreateVariables(511, 9)));
        var after = plan.EvaluateAssumedBound();

        // assert
        Assert.True(plan.HitCaseBudget);
        Assert.Equal(new CostEstimate(expectedFalseCost, 1, null), falseValue);
        Assert.Equal(new CostEstimate(45, 1, null), trueValue);
        Assert.Equal(new CostEstimate(45, 1, null), before);
        Assert.Equal(before, after);
    }

    [Fact]
    public void Evaluate_Should_PreserveCanonicalRounding_When_LargeFieldPrecedesSmallFields()
    {
        // arrange
        const string schema =
            """
            type Query {
              huge: Int @cost(weight: "9007199254740992")
              first: Int @cost(weight: "1")
              second: Int @cost(weight: "1")
            }
            """;
        const string operation =
            """
            query($z: Boolean!, $a: Boolean!, $m: Boolean!) {
              huge @include(if: $z)
              first @include(if: $a)
              second @include(if: $m)
            }
            """;
        var plan = Compile(schema, operation, 0, CaseBudgetExceededBehavior.EvaluatePerRequest);
        var values = new VariableValues(new Dictionary<string, IValueNode>
        {
            ["z"] = BooleanValueNode.True,
            ["a"] = BooleanValueNode.True,
            ["m"] = BooleanValueNode.True
        });

        // act
        var actual = plan.Evaluate(values);

        // assert
        Assert.Equal(
            BitConverter.DoubleToInt64Bits(9007199254740994d),
            BitConverter.DoubleToInt64Bits(actual.FieldCost));
        Assert.Equal([false, true], [actual.FieldCost > 9007199254740994d, actual.FieldCost > 9007199254740992d]);
    }

    [Fact]
    public void Evaluate_Should_PreserveNestedAndSharedConditions_When_RegionsListsAndInputsVary()
    {
        // arrange
        const string schema =
            """
            input Nested { value: Int @cost(weight: "0.125") }
            input Payload { items: [Nested] @cost(weight: "0.25") }
            type Item {
              value: Int @cost(weight: "0.5")
              tiny: Int @cost(weight: "0.25")
            }
            interface Node { value: Int }
            type A implements Node @cost(weight: "0.5") { value: Int @cost(weight: "1.25") }
            type B implements Node @cost(weight: "2") { value: Int @cost(weight: "0.75") }
            type Query {
              items(first: Int, payload: Payload): [Item]
                @listSize(assumedSize: 2, slicingArguments: ["first"])
              node: Node
              cheap: Int @cost(weight: "0.125")
            }
            """;
        const string operation =
            """
            query($v0: Boolean!, $v1: Boolean!, $v2: Boolean!, $n: Int, $payload: Payload) {
              items(first: $n, payload: $payload) @include(if: $v2) {
                value @include(if: $v0)
                ... @skip(if: $v1) { tiny }
              }
              node {
                ... on A @include(if: $v0) { value }
                ... on B @skip(if: $v1) { value }
              }
              shared: cheap @include(if: $v0)
            }
            """;
        var runtime = Compile(schema, operation, 0, CaseBudgetExceededBehavior.EvaluatePerRequest);
        var reference = Compile(schema, operation, int.MaxValue, CaseBudgetExceededBehavior.EvaluatePerRequest);
        var payload = new ObjectValueNode(new ObjectFieldNode(
            "items",
            new ListValueNode(
                new ObjectValueNode(new ObjectFieldNode("value", 3)),
                new ObjectValueNode(new ObjectFieldNode("value", 5)))));
        var expected = new List<long[]>();
        var actual = new List<long[]>();

        // act
        for (var mask = 0; mask < 8; mask++)
        {
            var values = CreateVariables(mask, 3);
            values["n"] = new IntValueNode(mask % 3);
            values["payload"] = payload;
            var variables = new VariableValues(values);
            expected.Add(Bits(reference.Evaluate(variables)));
            actual.Add(Bits(runtime.Evaluate(variables)));
            actual.Add(Bits(runtime.Evaluate(variables)));
            expected.Add(expected[^1]);
        }

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Evaluate_Should_PreserveFalseFallback_When_ValuesAreMissingNullOrNonBoolean()
    {
        // arrange
        var (schema, operation) = CreateIndependentOperation(9);
        var plan = Compile(schema, operation, 510, CaseBudgetExceededBehavior.EvaluatePerRequest);
        var values = CreateVariables(511, 9);
        values.Remove("v0");
        values["v1"] = NullValueNode.Default;
        values["v2"] = new StringValueNode("true");

        // act
        var actual = plan.Evaluate(new VariableValues(values));

        // assert
        Assert.Equal(new CostEstimate(39, 1, null), actual);
    }

    private static CostPlan Compile(
        string schemaSource,
        string operationSource,
        int budget,
        CaseBudgetExceededBehavior behavior)
    {
        var schema = SchemaParser.Parse(Directives + schemaSource);
        var index = CostSchemaIndex.Create(
            schema,
            new CostSchemaIndexOptions { CaseBudget = budget, CaseBudgetExceededBehavior = behavior });
        var document = Utf8GraphQLParser.Parse(operationSource);
        return CostPlanCompiler.Compile(
            index,
            document,
            document.Definitions.OfType<OperationDefinitionNode>().Single(),
            CostAnalyses.Cost);
    }

    private static (string Schema, string Operation) CreateIndependentOperation(int count)
    {
        var schema = new StringBuilder("type Query {");
        var variables = new StringBuilder();
        var fields = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            schema.Append($" f{i}: Int @cost(weight: \"{i + 1}\")");
            variables.Append($"$v{i}: Boolean! ");
            fields.Append($" f{i} @include(if: $v{i})");
        }
        return (schema.Append(" }").ToString(), $"query({variables}) {{{fields} }}");
    }

    private static Dictionary<string, IValueNode> CreateVariables(int mask, int count)
        => Enumerable.Range(0, count).ToDictionary(
            i => $"v{i}",
            i => (IValueNode)((mask & (1 << i)) != 0 ? BooleanValueNode.True : BooleanValueNode.False));

    private static double ExpectedFieldCost(int mask, int count)
        => Enumerable.Range(0, count).Where(i => (mask & (1 << i)) != 0).Sum(i => i + 1d);

    private static long[] Bits(CostEstimate value)
        => [BitConverter.DoubleToInt64Bits(value.FieldCost), BitConverter.DoubleToInt64Bits(value.TypeCost)];

    private sealed class VariableValues(IReadOnlyDictionary<string, IValueNode> values) : ICostVariableValues
    {
        public bool TryGetValue(string name, out IValueNode? value)
            => values.TryGetValue(name, out value);
    }
}
