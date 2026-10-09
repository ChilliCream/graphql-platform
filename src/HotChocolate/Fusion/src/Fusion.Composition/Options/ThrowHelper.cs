namespace HotChocolate.Fusion.Options;

internal static class ThrowHelper
{
    public static ArgumentOutOfRangeException InvalidDefaultListSize(int value)
        => new(
            nameof(SourceSchemaMergerOptions.DefaultListSize),
            value,
            "The value must be non-negative.");

    public static ArgumentOutOfRangeException UnexpectedCostCoordinateKind(CostCoordinateKind kind)
        => new(nameof(kind));
}
