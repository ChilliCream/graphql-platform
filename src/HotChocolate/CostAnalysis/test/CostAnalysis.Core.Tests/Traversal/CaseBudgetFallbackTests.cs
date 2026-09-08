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
