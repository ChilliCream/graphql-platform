using HotChocolate.CostAnalysis.Utilities;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

internal static class ThrowHelper
{
    public static ArgumentOutOfRangeException InvalidCostOptionValue(
        string optionName,
        double value)
        => new(
            optionName,
            value,
            "The value must be a non-negative finite number or positive infinity.");

    public static GraphQLException ExactlyOneSlicingArgMustBeDefined(
        FieldNode sourceNode,
        IList<ISyntaxNode> path)
        => new(ErrorHelper.ExactlyOneSlicingArgMustBeDefined(sourceNode, path));
}
