using HotChocolate.CostAnalysis.Properties;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis.Utilities;

internal static class ErrorHelper
{
    public static IExecutionResult MaxFieldCostReached(
        CostMetrics costMetrics,
        double maxFieldCost,
        bool reportMetrics)
        => ResultHelper.CreateError(
            new Error(
                CostAnalysisResources.ErrorHelper_MaxFieldCostReached,
                ErrorCodes.Execution.CostExceeded,
                extensions: new Dictionary<string, object?>
                {
                    { "fieldCost", costMetrics.FieldCost },
                    { nameof(maxFieldCost), maxFieldCost }
                }),
            reportMetrics ? costMetrics : null);

    public static IExecutionResult MaxTypeCostReached(
        CostMetrics costMetrics,
        double maxTypeCost,
        bool reportMetrics)
        => ResultHelper.CreateError(
            new Error(
                CostAnalysisResources.ErrorHelper_MaxTypeCostReached,
                ErrorCodes.Execution.CostExceeded,
                extensions: new Dictionary<string, object?>
                {
                    { "typeCost", costMetrics.TypeCost },
                    { nameof(maxTypeCost), maxTypeCost }
                }),
            reportMetrics ? costMetrics : null);

    public static IError ExactlyOneSlicingArgMustBeDefined(
        FieldNode fieldNode,
        IList<ISyntaxNode> path)
    {
        var errorPath = new List<object>();

        foreach (var node in path)
        {
            if (node is FieldNode field)
            {
                errorPath.Add(field.Name.Value);
            }
        }

        errorPath.Add(fieldNode.Name.Value);

        return ErrorBuilder.New()
            .SetMessage("Exactly one slicing argument must be defined.")
            .SetCode(ErrorCodes.Execution.OneSlicingArgumentRequired)
            .AddLocation(fieldNode)
            .SetPath(errorPath)
            .Build();
    }
}
