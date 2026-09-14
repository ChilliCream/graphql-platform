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
}
