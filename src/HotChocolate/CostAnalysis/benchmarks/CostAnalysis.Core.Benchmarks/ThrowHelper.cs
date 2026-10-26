using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.CostAnalysis;

internal static class ThrowHelper
{
    [DoesNotReturn]
    public static void UnexpectedBudgetResult(
        int variableCount,
        int caseBudget,
        bool expected,
        bool actual)
        => throw new InvalidOperationException(
            $"The {variableCount}-variable adversarial plan with budget {caseBudget} "
            + $"reported HitCaseBudget={actual}, expected {expected}.");
}
