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
}
