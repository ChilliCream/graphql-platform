using System.Collections.Immutable;
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
    {
        var extensions = ImmutableSortedDictionary.CreateBuilder<string, object?>();
        extensions.Add("code", ErrorCodes.Execution.CostExceeded);
        extensions.Add("fieldCost", ResultHelper.FormatValue(costMetrics.FieldCost));
        extensions.Add("maxFieldCost", ResultHelper.FormatValue(maxFieldCost));

        return ResultHelper.CreateError(
            new Error
            {
                Message = CostAnalysisResources.ErrorHelper_MaxFieldCostReached,
                Extensions = extensions.ToImmutable()
            },
            reportMetrics ? costMetrics : null);
    }

    public static IExecutionResult MaxTypeCostReached(
        CostMetrics costMetrics,
        double maxTypeCost,
        bool reportMetrics)
    {
        var extensions = ImmutableSortedDictionary.CreateBuilder<string, object?>();
        extensions.Add("code", ErrorCodes.Execution.CostExceeded);
        extensions.Add("typeCost", ResultHelper.FormatValue(costMetrics.TypeCost));
        extensions.Add("maxTypeCost", ResultHelper.FormatValue(maxTypeCost));

        return ResultHelper.CreateError(
            new Error
            {
                Message = CostAnalysisResources.ErrorHelper_MaxTypeCostReached,
                Extensions = extensions.ToImmutable()
            },
            reportMetrics ? costMetrics : null);
    }

    public static IExecutionResult MaxResponseSizeReached(
        CostMetrics costMetrics,
        double maxResponseSize,
        double maxAllowedResponseSize,
        bool reportMetrics)
    {
        var extensions = ImmutableSortedDictionary.CreateBuilder<string, object?>();
        extensions.Add("code", ErrorCodes.Execution.CostExceeded);
        extensions.Add("maxResponseSize", ResultHelper.FormatValue(maxResponseSize));
        extensions.Add("maxAllowedResponseSize", ResultHelper.FormatValue(maxAllowedResponseSize));

        return ResultHelper.CreateError(
            new Error
            {
                Message = CostAnalysisResources.ErrorHelper_MaxResponseSizeReached,
                Extensions = extensions.ToImmutable()
            },
            reportMetrics ? costMetrics : null);
    }

    public static IExecutionResult StateInvalidForCostAnalysis()
        => ResultHelper.CreateError(
            ErrorBuilder.New()
                .SetMessage(CostAnalysisResources.ErrorHelper_StateInvalidForCostAnalysis)
                .SetCode(ErrorCodes.Execution.CostStateInvalid)
                .Build(),
            null);

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
            .SetPath(Path.FromList(errorPath))
            .Build();
    }
}
