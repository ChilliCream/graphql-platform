using System.Globalization;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Pins the case-budget fallback's structural bound: once the budget is
/// exhausted, the remaining pending variables collapse into one envelope
/// leaf instead of growing the decision structure exponentially in their
/// count.
/// </summary>
public class CaseBudgetFallbackTests
{
    private const int VariableCount = 14;

    [Fact]
    public void Evaluate_Should_Produce_A_Leaf_Sized_Independently_Of_Gated_Field_Count_When_The_Budget_Is_Exhausted()
    {
        // arrange: 14 singly @include-gated Int fields on Query, no exact cases allowed
        var (sdl, operation) = GenerateOperation();

        // act
        var budgeted = TraversalTestHelpers.EvaluateOperation(sdl, operation, caseBudget: 0);
        var unbudgeted = TraversalTestHelpers.EvaluateOperation(sdl, operation, caseBudget: int.MaxValue);
        var budgetedAllTrue = budgeted.Resolve(_ => true);
        var unbudgetedAllTrue = unbudgeted.Resolve(_ => true);
        var budgetedAllFalse = budgeted.Resolve(_ => false);
        var unbudgetedAllFalse = unbudgeted.Resolve(_ => false);

        // assert: one leaf regardless of k, and it overestimates both extreme assignments
        Assert.IsType<LeafDecision<(double TypeCost, double FieldCost)>>(budgeted);
        Assert.True(budgetedAllTrue.TypeCost >= unbudgetedAllTrue.TypeCost && budgetedAllTrue.FieldCost >= unbudgetedAllTrue.FieldCost);
        Assert.True(budgetedAllFalse.TypeCost >= unbudgetedAllFalse.TypeCost && budgetedAllFalse.FieldCost >= unbudgetedAllFalse.FieldCost);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Evaluate_Should_MatchEveryAssignment_When_IndependentCaseSetFitsBudget(
        int variableCount)
    {
        // arrange
        var (sdl, operation) = GenerateOperation(variableCount);
        var decision = TraversalTestHelpers.EvaluateOperation(
            sdl,
            operation,
            caseBudget: 4096);

        // act
        var estimates = new List<(double TypeCost, double FieldCost)>();
        var expected = new List<(double TypeCost, double FieldCost)>();

        for (var mask = 0; mask < 1 << variableCount; mask++)
        {
            estimates.Add(decision.Resolve(name => (mask & (1 << int.Parse(name.AsSpan(1), CultureInfo.InvariantCulture))) != 0));
            var fieldCost = 0.0;

            for (var variable = 0; variable < variableCount; variable++)
            {
                if ((mask & (1 << variable)) != 0)
                {
                    fieldCost += variable + 1;
                }
            }

            expected.Add((1.0, fieldCost));
        }

        // assert
        Assert.Equal(expected, estimates);
    }

    [Theory]
    [InlineData(13)]
    [InlineData(20)]
    public void Evaluate_Should_EnterFallbackBeforeGrowingIndependentDecisionBeyondBudget(
        int variableCount)
    {
        // arrange
        var (sdl, operation) = GenerateOperation(variableCount);

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(
            sdl,
            operation,
            caseBudget: 4096);

        // assert
        Assert.IsType<LeafDecision<(double TypeCost, double FieldCost)>>(decision);
    }

    [Fact]
    public void Evaluate_Should_KeepCanonicalK13TypeRegionsFactored_When_DefaultBudgetBinds()
    {
        // arrange
        var (sdl, operation) = GenerateCorrelatedTypeRegionOperation(13);

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(
            sdl,
            operation,
            caseBudget: 4096);

        // assert
        Assert.IsType<JoinDecision<(double TypeCost, double FieldCost)>>(decision);
        Assert.Equal((2.0, 92.0), decision.Resolve(_ => true));
        Assert.Equal((2.0, 92.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Bound_The_Decision_Size_By_The_Budget_When_Variables_Live_In_Sibling_Boundaries()
    {
        // arrange: 14 sibling object fields, each with its own gated child selection
        const int caseBudget = 4096;
        var (sdl, operation) = GenerateSiblingBoundaryOperation();

        // act
        var budgeted = TraversalTestHelpers.EvaluateOperation(sdl, operation, caseBudget: caseBudget);
        var unbudgeted = TraversalTestHelpers.EvaluateOperation(sdl, operation, caseBudget: int.MaxValue);
        var budgetedAllTrue = budgeted.Resolve(_ => true);
        var unbudgetedAllTrue = unbudgeted.Resolve(_ => true);
        var budgetedAllFalse = budgeted.Resolve(_ => false);
        var unbudgetedAllFalse = unbudgeted.Resolve(_ => false);

        // assert: the budgeted structure stays bounded and overestimates both extreme assignments
        Assert.True(CountNodes(budgeted) <= (2 * caseBudget) + 1);
        Assert.True(budgetedAllTrue.TypeCost >= unbudgetedAllTrue.TypeCost && budgetedAllTrue.FieldCost >= unbudgetedAllTrue.FieldCost);
        Assert.True(budgetedAllFalse.TypeCost >= unbudgetedAllFalse.TypeCost && budgetedAllFalse.FieldCost >= unbudgetedAllFalse.FieldCost);
    }

    [Fact]
    public void Evaluate_Should_JoinPairOutputs_When_CaseBudgetIsExhausted()
    {
        // arrange
        const string sdl =
            """
            interface Node { edges: [Edge] }
            type A implements Node { edges: [Edge] @cost(weight: "100") @listSize(assumedSize: 1) }
            type B implements Node { edges: [Edge] @cost(weight: "1") @listSize(assumedSize: 100) }
            type Edge { value: Int @cost(weight: "1") }
            type Container { node: Node }
            type Query { container: Container }
            """;
        const string operation = "query($include: Boolean!) { container { node { edges @include(if: $include) { value } } } }";
        var algebra = new CostAlgebra(ConditionTreeTestHelpers.BuildSnapshot(sdl));

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation, algebra, caseBudget: 0);

        // assert
        Assert.Equal(new CostEstimate(103.0, 103.0, null), decision.Resolve(_ => false));
    }

    [Fact]
    public void Compile_Should_UseCanonicalVariableOrder_When_MergedFieldOccurrencesAreReordered()
    {
        // arrange
        const string sdl =
            """
            type Obj {
              expensive: Int @cost(weight: "10")
              cheap: Int @cost(weight: "1")
            }
            type Query { obj: Obj }
            """;
        var first = CompilePlan(
            sdl,
            "query($a:Boolean!,$z:Boolean!){obj @include(if:$z){expensive} obj @include(if:$a){cheap}}");
        var reordered = CompilePlan(
            sdl,
            "query($a:Boolean!,$z:Boolean!){obj @include(if:$a){cheap} obj @include(if:$z){expensive}}");
        CostEstimate[] expected =
        [
            new(11.0, 2.0, null),
            new(12.0, 2.0, null),
            new(11.0, 2.0, null),
            new(12.0, 2.0, null)
        ];

        // act
        var firstMatrix = EvaluateMatrix(first, "a", "z");
        var reorderedMatrix = EvaluateMatrix(reordered, "a", "z");

        // assert
        Assert.Equal([true, true], [first.HitCaseBudget, reordered.HitCaseBudget]);
        Assert.Equal([new CostEstimate(12.0, 2.0, null), new CostEstimate(12.0, 2.0, null)], [first.EvaluateStaticBound(), reordered.EvaluateStaticBound()]);
        Assert.Equal(expected, firstMatrix);
        Assert.Equal(expected, reorderedMatrix);
    }

    [Fact]
    public void Compile_Should_UseCanonicalVariableOrder_When_NestedOccurrencesAreReordered()
    {
        // arrange
        const string sdl =
            """
            type Obj {
              expensive: Int @cost(weight: "10")
              cheap: Int @cost(weight: "1")
            }
            type Outer { obj: Obj }
            type Query { outer: Outer }
            """;
        var first = CompilePlan(
            sdl,
            "query($a:Boolean!,$z:Boolean!){outer{obj @include(if:$z){expensive} obj @include(if:$a){cheap}}}");
        var reordered = CompilePlan(
            sdl,
            "query($a:Boolean!,$z:Boolean!){outer{obj @include(if:$a){cheap} obj @include(if:$z){expensive}}}");
        CostEstimate[] expected =
        [
            new(12.0, 3.0, null),
            new(13.0, 3.0, null),
            new(12.0, 3.0, null),
            new(13.0, 3.0, null)
        ];

        // act
        var firstMatrix = EvaluateMatrix(first, "a", "z");
        var reorderedMatrix = EvaluateMatrix(reordered, "a", "z");

        // assert
        Assert.Equal([true, true], [first.HitCaseBudget, reordered.HitCaseBudget]);
        Assert.Equal([new CostEstimate(13.0, 3.0, null), new CostEstimate(13.0, 3.0, null)], [first.EvaluateStaticBound(), reordered.EvaluateStaticBound()]);
        Assert.Equal(expected, firstMatrix);
        Assert.Equal(expected, reorderedMatrix);
    }

    [Fact]
    public void Compile_Should_SpendOnce_When_VariableRepeatsAcrossSiblingAndNestedBoundaries()
    {
        // arrange
        const string sdl =
            """
            type Obj { expensive: Int @cost(weight: "10") }
            type Query {
              sibling: Int @cost(weight: "2")
              obj: Obj
            }
            """;
        var first = CompilePlan(
            sdl,
            "query($a:Boolean!){sibling @include(if:$a) obj @include(if:$a){expensive @include(if:$a)}}");
        var reordered = CompilePlan(
            sdl,
            "query($a:Boolean!){obj @include(if:$a){expensive @include(if:$a)} sibling @include(if:$a)}");
        CostEstimate[] expected = [new(0.0, 1.0, null), new(13.0, 2.0, null)];

        // act
        var firstMatrix = EvaluateMatrix(first, "a");
        var reorderedMatrix = EvaluateMatrix(reordered, "a");

        // assert
        Assert.Equal([false, false], [first.HitCaseBudget, reordered.HitCaseBudget]);
        Assert.Equal([new CostEstimate(13.0, 2.0, null), new CostEstimate(13.0, 2.0, null)], [first.EvaluateStaticBound(), reordered.EvaluateStaticBound()]);
        Assert.Equal(expected, firstMatrix);
        Assert.Equal(expected, reorderedMatrix);
    }

    [Fact]
    public void Compile_Should_UseCanonicalRegionOrder_When_FactoredRegionsAreReordered()
    {
        // arrange
        const string sdl =
            """
            union Result = A | B
            type A { obj: AObj }
            type B { obj: BObj }
            type AObj { expensive: Int @cost(weight: "10") cheap: Int @cost(weight: "1") }
            type BObj { expensive: Int @cost(weight: "2") cheap: Int @cost(weight: "1") }
            type Query { result: Result }
            """;
        const string firstOperation =
            "query($a:Boolean!,$b:Boolean!,$y:Boolean!,$z:Boolean!){result{... on A{obj @include(if:$z){expensive} obj @include(if:$a){cheap}} ... on B{obj @include(if:$y){expensive} obj @include(if:$b){cheap}}}}";
        const string reorderedOperation =
            "query($a:Boolean!,$b:Boolean!,$y:Boolean!,$z:Boolean!){result{... on B{obj @include(if:$b){cheap} obj @include(if:$y){expensive}} ... on A{obj @include(if:$a){cheap} obj @include(if:$z){expensive}}}}";
        var first = CompilePlan(sdl, firstOperation);
        var reordered = CompilePlan(sdl, reorderedOperation);

        // act
        var firstMatrix = EvaluateMatrix(first, "a", "b", "y", "z");
        var reorderedMatrix = EvaluateMatrix(reordered, "a", "b", "y", "z");
        var firstDecision = TraversalTestHelpers.EvaluateOperation(sdl, firstOperation, caseBudget: 1);
        var reorderedDecision = TraversalTestHelpers.EvaluateOperation(sdl, reorderedOperation, caseBudget: 1);

        // assert
        Assert.Equal([true, true], [first.HitCaseBudget, reordered.HitCaseBudget]);
        Assert.Equal(first.EvaluateStaticBound(), reordered.EvaluateStaticBound());
        Assert.Equal(firstMatrix, reorderedMatrix);
        Assert.IsType<JoinDecision<(double TypeCost, double FieldCost)>>(firstDecision);
        Assert.IsType<JoinDecision<(double TypeCost, double FieldCost)>>(reorderedDecision);
    }

    /// <summary>
    /// Counts every node of a <see cref="BooleanDecision{T}"/>, splits and
    /// leaves alike.
    /// </summary>
    private static int CountNodes<T>(BooleanDecision<T> decision)
        => decision is SplitDecision<T> split
            ? 1 + CountNodes(split.WhenFalse) + CountNodes(split.WhenTrue)
            : 1;

    private static CostPlan CompilePlan(
        string sdl,
        string operationSource)
    {
        const string directives =
            """
            directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
            directive @listSize(assumedSize: Int, slicingArguments: [String!], slicingArgumentDefaultValue: Float, sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION
            """;
        var schema = SchemaParser.Parse(directives + "\n" + sdl);
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions { CaseBudget = 1 });
        var document = Utf8GraphQLParser.Parse(operationSource);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        return CostPlanCompiler.Compile(snapshot, document, operation, CostAnalyses.Cost);
    }

    private static CostEstimate[] EvaluateMatrix(
        CostPlan plan,
        params string[] variables)
    {
        var result = new CostEstimate[1 << variables.Length];

        for (var mask = 0; mask < result.Length; mask++)
        {
            var values = new Dictionary<string, IValueNode>(variables.Length);

            for (var index = 0; index < variables.Length; index++)
            {
                values.Add(
                    variables[index],
                    (mask & (1 << index)) == 0
                        ? BooleanValueNode.False
                        : BooleanValueNode.True);
            }

            result[mask] = plan.Evaluate(new TestVariableValues(values));
        }

        return result;
    }

    private sealed class TestVariableValues(
        IReadOnlyDictionary<string, IValueNode> values) : ICostVariableValues
    {
        public bool TryGetValue(string name, out IValueNode? value)
            => values.TryGetValue(name, out value);
    }

    /// <summary>
    /// Generates a schema with <see cref="VariableCount"/> sibling object
    /// fields on Query, each returning a type with one Int field gated by
    /// its own <c>@include</c> variable, so every variable lives in its own
    /// boundary rather than a shared one.
    /// </summary>
    private static (string Sdl, string Operation) GenerateSiblingBoundaryOperation()
    {
        var sdl = new StringBuilder("type C { x: Int @cost(weight: \"1\") } type Query {");
        var variableDeclarations = new StringBuilder();
        var selections = new StringBuilder();

        for (var i = 0; i < VariableCount; i++)
        {
            sdl.Append(" f").Append(i).Append(": C");

            if (i > 0)
            {
                variableDeclarations.Append(", ");
            }

            variableDeclarations.Append('$').Append('v').Append(i).Append(": Boolean!");
            selections.Append(" f").Append(i).Append(" { x @include(if: $v").Append(i).Append(") }");
        }

        sdl.Append(" }");

        var operation = new StringBuilder("query(").Append(variableDeclarations).Append(") {").Append(selections).Append(" }");
        return (sdl.ToString(), operation.ToString());
    }

    /// <summary>
    /// Generates a schema with <see cref="VariableCount"/> top-level Int
    /// fields on Query, each gated by its own <c>@include</c> variable.
    /// </summary>
    private static (string Sdl, string Operation) GenerateOperation()
        => GenerateOperation(VariableCount);

    private static (string Sdl, string Operation) GenerateOperation(int variableCount)
    {
        var sdl = new StringBuilder("type Query {");
        var variableDeclarations = new StringBuilder();
        var selections = new StringBuilder();

        for (var i = 0; i < variableCount; i++)
        {
            var weight = (i + 1).ToString(CultureInfo.InvariantCulture);
            sdl.Append(" f").Append(i).Append(": Int @cost(weight: \"").Append(weight).Append("\")");

            if (i > 0)
            {
                variableDeclarations.Append(", ");
            }

            variableDeclarations.Append('$').Append('v').Append(i).Append(": Boolean!");
            selections.Append(" f").Append(i).Append(" @include(if: $v").Append(i).Append(')');
        }

        sdl.Append(" }");

        var operation = new StringBuilder("query(").Append(variableDeclarations).Append(") {").Append(selections).Append(" }");
        return (sdl.ToString(), operation.ToString());
    }

    private static (string Sdl, string Operation) GenerateCorrelatedTypeRegionOperation(
        int variableCount)
    {
        var sdl = new StringBuilder("union Result = Left | Right type Left {");
        var rightFields = new StringBuilder();
        var variableDeclarations = new StringBuilder();
        var leftSelections = new StringBuilder();
        var rightSelections = new StringBuilder();

        for (var i = 0; i < variableCount; i++)
        {
            var weight = (i + 1).ToString(CultureInfo.InvariantCulture);
            sdl.Append(" f").Append(i).Append(": Int @cost(weight: \"").Append(weight).Append("\")");
            rightFields.Append(" f").Append(i).Append(": Int @cost(weight: \"").Append(weight).Append("\")");

            if (i > 0)
            {
                variableDeclarations.Append(',');
            }

            variableDeclarations.Append("$v").Append(i).Append(":Boolean!");
            leftSelections.Append(" l").Append(i).Append(":f").Append(i).Append(" @include(if:$v").Append(i).Append(')');
            rightSelections.Append(" r").Append(i).Append(":f").Append(i).Append(" @skip(if:$v").Append(i).Append(')');
        }

        sdl.Append(" } type Right {").Append(rightFields).Append(" } type Query { result: Result }");
        var operation = new StringBuilder("query(")
            .Append(variableDeclarations)
            .Append(") { result { ... on Left {")
            .Append(leftSelections)
            .Append(" } ... on Right {")
            .Append(rightSelections)
            .Append(" } } }");
        return (sdl.ToString(), operation.ToString());
    }
}
