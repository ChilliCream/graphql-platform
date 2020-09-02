using System;

namespace HotChocolate.Types.Filters
{
    public class FilterNamingConventionPascalCase : FilterNamingConventionBase
    {
        public override NameString ArgumentName => "Where";

        public override NameString CreateFieldName(
            FilterFieldDefintion definition,
            FilterOperationKind kind)
        {
            switch (kind)
            {
                case FilterOperationKind.Equals:
                    return GetNameForDefinition(definition);
                case FilterOperationKind.NotEquals:
                    return GetNameForDefinition(definition) + "_Not";

                case FilterOperationKind.Contains:
                    return GetNameForDefinition(definition) + "_Contains";
                case FilterOperationKind.NotContains:
                    return GetNameForDefinition(definition) + "_Not_Contains";

                case FilterOperationKind.In:
                    return GetNameForDefinition(definition) + "_In";
                case FilterOperationKind.NotIn:
                    return GetNameForDefinition(definition) + "_Not_In";

                case FilterOperationKind.StartsWith:
                    return GetNameForDefinition(definition) + "_StartsWith";
                case FilterOperationKind.NotStartsWith:
                    return GetNameForDefinition(definition) + "_Not_StartsWith";

                case FilterOperationKind.EndsWith:
                    return GetNameForDefinition(definition) + "_EndsWith";
                case FilterOperationKind.NotEndsWith:
                    return GetNameForDefinition(definition) + "_Not_EndsWith";

                case FilterOperationKind.GreaterThan:
                    return GetNameForDefinition(definition) + "_Gt";
                case FilterOperationKind.NotGreaterThan:
                    return GetNameForDefinition(definition) + "_Not_Gt";

                case FilterOperationKind.GreaterThanOrEquals:
                    return GetNameForDefinition(definition) + "_Gte";
                case FilterOperationKind.NotGreaterThanOrEquals:
                    return GetNameForDefinition(definition) + "_Not_Gte";

                case FilterOperationKind.LowerThan:
                    return GetNameForDefinition(definition) + "_Lt";
                case FilterOperationKind.NotLowerThan:
                    return GetNameForDefinition(definition) + "_Not_Lt";

                case FilterOperationKind.LowerThanOrEquals:
                    return GetNameForDefinition(definition) + "_Lte";
                case FilterOperationKind.NotLowerThanOrEquals:
                    return GetNameForDefinition(definition) + "_Not_Lte";

                case FilterOperationKind.Object:
                    return GetNameForDefinition(definition);

                case FilterOperationKind.ArraySome:
                    return definition.Name + "_Some";
                case FilterOperationKind.ArrayNone:
                    return definition.Name + "_None";
                case FilterOperationKind.ArrayAll:
                    return definition.Name + "_All";
                case FilterOperationKind.ArrayAny:
                    return definition.Name + "_Any";

                default:
                    throw new NotSupportedException();
            }
        }

        private static string GetNameForDefinition(FilterFieldDefintion definition) =>
            definition.Name.Value is { Length: > 1 } name
                ? name.Substring(0, 1).ToUpperInvariant() + name.Substring(1)
                : definition.Name.Value.ToUpperInvariant();
    }
}
