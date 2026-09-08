using System.Globalization;
using System.Text;

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

    /// <summary>
    /// Counts every node of a <see cref="BooleanDecision{T}"/>, splits and
    /// leaves alike.
    /// </summary>
    private static int CountNodes<T>(BooleanDecision<T> decision)
        => decision is SplitDecision<T> split
            ? 1 + CountNodes(split.WhenFalse) + CountNodes(split.WhenTrue)
            : 1;

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
    {
        var sdl = new StringBuilder("type Query {");
        var variableDeclarations = new StringBuilder();
        var selections = new StringBuilder();

        for (var i = 0; i < VariableCount; i++)
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
}
