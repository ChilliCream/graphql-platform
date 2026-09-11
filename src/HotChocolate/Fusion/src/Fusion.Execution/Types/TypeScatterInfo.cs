namespace HotChocolate.Fusion.Types;

internal readonly record struct TypeScatterInfo(
    int TotalFields,
    int SchemaCount,
    int MaxCoverage,
    double ScatterRatio);
