using HotChocolate.Types;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Options;

internal static class ThrowHelper
{
    public static ArgumentOutOfRangeException InvalidDefaultListSize(int value)
        => new(
            nameof(SourceSchemaMergerOptions.DefaultListSize),
            value,
            "The value must be non-negative.");

    public static ArgumentOutOfRangeException InvalidMaxFieldCost(double value)
        => new(
            nameof(SourceSchemaMergerOptions.MaxFieldCost),
            value,
            "The value must be a non-negative finite number.");

    public static ArgumentOutOfRangeException InvalidMaxTypeCost(double value)
        => new(
            nameof(SourceSchemaMergerOptions.MaxTypeCost),
            value,
            "The value must be a non-negative finite number.");

    public static ArgumentOutOfRangeException InvalidMaxResponseSize(double value)
        => new(
            nameof(SourceSchemaMergerOptions.MaxResponseSize),
            value,
            "The value must be a non-negative finite number.");

    public static ArgumentOutOfRangeException InvalidCostPlanCacheSize(int value)
        => new(
            nameof(SourceSchemaMergerOptions.CostPlanCacheSize),
            value,
            "The value must be positive.");

    public static InvalidOperationException UnexpectedCaseBudgetExceededBehavior(
        CaseBudgetExceededBehavior behavior)
        => new($"The case budget exceeded behavior '{behavior}' is invalid.");

    public static ArgumentOutOfRangeException UnexpectedCostCoordinateKind(CostCoordinateKind kind)
        => new(nameof(kind));

    public static InvalidOperationException PolicyDirectiveArgumentInvalid(
        string directiveName,
        string argumentName)
        => new(string.Format(PolicyDirective_Argument_Invalid, directiveName, argumentName));

    public static InvalidOperationException UnexpectedTypeKind(TypeKind kind)
        => new(string.Format(PolicyInheritance_UnexpectedTypeKind, kind));

    public static InvalidOperationException UnknownSourceSchema(string schemaName)
        => new($"The source schema '{schemaName}' has no index in the schema set table.");
}
