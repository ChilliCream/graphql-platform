using System;

namespace HotChocolate.Types.Filters;

[Obsolete("Use HotChocolate.Data.")]
public class FilterNamingConventionPascalCase : FilterNamingConventionBase
{
    public override string ArgumentName => "Where";

    public override string CreateFieldName(
        FilterFieldDefintion definition,
        FilterOperationKind kind)
        => kind switch
        {
            FilterOperationKind.Equals => GetNameForDefinition(definition),
            FilterOperationKind.NotEquals => GetNameForDefinition(definition) + "_Not",
            FilterOperationKind.Contains => GetNameForDefinition(definition) + "_Contains",
            FilterOperationKind.NotContains => GetNameForDefinition(definition) + "_Not_Contains",
            FilterOperationKind.In => GetNameForDefinition(definition) + "_In",
            FilterOperationKind.NotIn => GetNameForDefinition(definition) + "_Not_In",
            FilterOperationKind.StartsWith => GetNameForDefinition(definition) + "_StartsWith",
            FilterOperationKind.NotStartsWith => GetNameForDefinition(definition) +
                "_Not_StartsWith",
            FilterOperationKind.EndsWith => GetNameForDefinition(definition) + "_EndsWith",
            FilterOperationKind.NotEndsWith => GetNameForDefinition(definition) + "_Not_EndsWith",
            FilterOperationKind.GreaterThan => GetNameForDefinition(definition) + "_Gt",
            FilterOperationKind.NotGreaterThan => GetNameForDefinition(definition) + "_Not_Gt",
            FilterOperationKind.GreaterThanOrEquals => GetNameForDefinition(definition) + "_Gte",
            FilterOperationKind.NotGreaterThanOrEquals => GetNameForDefinition(definition) +
                "_Not_Gte",
            FilterOperationKind.LowerThan => GetNameForDefinition(definition) + "_Lt",
            FilterOperationKind.NotLowerThan => GetNameForDefinition(definition) + "_Not_Lt",
            FilterOperationKind.LowerThanOrEquals => GetNameForDefinition(definition) + "_Lte",
            FilterOperationKind.NotLowerThanOrEquals => GetNameForDefinition(definition) +
                "_Not_Lte",
            FilterOperationKind.Object => GetNameForDefinition(definition),
            FilterOperationKind.ArraySome => definition.Name + "_Some",
            FilterOperationKind.ArrayNone => definition.Name + "_None",
            FilterOperationKind.ArrayAll => definition.Name + "_All",
            FilterOperationKind.ArrayAny => definition.Name + "_Any",
            _ => throw new NotSupportedException()
        };

    private static string GetNameForDefinition(FilterFieldDefintion definition) =>
        definition.Name is { Length: > 1 } name
            ? name.Substring(0, 1).ToUpperInvariant() + name.Substring(1)
            : definition.Name.ToUpperInvariant();
}
