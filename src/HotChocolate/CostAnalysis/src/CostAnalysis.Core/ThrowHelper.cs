using System.Globalization;
using HotChocolate.Language;
using static HotChocolate.CostAnalysis.Properties.CostAnalysisCoreResources;

namespace HotChocolate.CostAnalysis;

internal static class ThrowHelper
{
    public static NotImplementedException NotImplemented()
        => new(ThrowHelper_NotImplemented);

    public static InvalidOperationException InvalidCostWeight(SchemaCoordinate coordinate, IValueNode value)
        => new(string.Format(
            CultureInfo.InvariantCulture,
            ThrowHelper_InvalidCostWeight,
            coordinate,
            value.ToString()));

    public static InvalidOperationException InvalidListSizeArgument(
        SchemaCoordinate coordinate,
        string argumentName,
        IValueNode value)
        => new(string.Format(
            CultureInfo.InvariantCulture,
            ThrowHelper_InvalidListSizeArgument,
            argumentName,
            coordinate,
            value.ToString()));

    public static InvalidOperationException OperationTypeNotDefined(OperationType operation)
        => new($"The schema does not define a root type for '{operation}'.");

    public static ArgumentOutOfRangeException InvalidAnalyses(CostAnalyses analyses)
        => new(nameof(analyses), analyses, "At least one known cost analysis must be requested.");

    public static InvalidOperationException UnexpectedDecision()
        => new("The compiled Boolean decision has an unsupported node type.");
}
