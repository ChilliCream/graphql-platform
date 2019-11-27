using System;

namespace HotChocolate.Types.Filters
{
    public class FilterNamingConventionSnakeCase : IFilterNamingConvention
    {
        public NameString CreateFieldName(FilterFieldDefintion definition, FilterOperationKind kind)
        {
            switch (kind)
            {
                case FilterOperationKind.Equals:
                    return definition.Name;
                case FilterOperationKind.NotEquals:
                    return definition.Name + "_not";

                case FilterOperationKind.Contains:
                    return definition.Name + "_contains";
                case FilterOperationKind.NotContains:
                    return definition.Name + "_not_contains";

                case FilterOperationKind.In:
                    return definition.Name + "_in";
                case FilterOperationKind.NotIn:
                    return definition.Name + "_not_in";

                case FilterOperationKind.StartsWith:
                    return definition.Name + "_starts_with";
                case FilterOperationKind.NotStartsWith:
                    return definition.Name + "_not_starts_with";

                case FilterOperationKind.EndsWith:
                    return definition.Name + "_ends_with";
                case FilterOperationKind.NotEndsWith:
                    return definition.Name + "_not_ends_with";

                case FilterOperationKind.GreaterThan:
                    return definition.Name + "_gt";
                case FilterOperationKind.NotGreaterThan:
                    return definition.Name + "_not_gt";

                case FilterOperationKind.GreaterThanOrEquals:
                    return definition.Name + "_gte";
                case FilterOperationKind.NotGreaterThanOrEquals:
                    return definition.Name + "_not_gte";

                case FilterOperationKind.LowerThan:
                    return definition.Name + "_lt";
                case FilterOperationKind.NotLowerThan:
                    return definition.Name + "_not_lt";

                case FilterOperationKind.LowerThanOrEquals:
                    return definition.Name + "_lte";
                case FilterOperationKind.NotLowerThanOrEquals:
                    return definition.Name + "_not_lte";

                case FilterOperationKind.Object:
                    return definition.Name;

                case FilterOperationKind.ArraySome:
                    return definition.Name + "_some";
                case FilterOperationKind.ArrayNone:
                    return definition.Name + "_none";
                case FilterOperationKind.ArrayAll:
                    return definition.Name + "_all";
                case FilterOperationKind.ArrayAny:
                    return definition.Name + "_any";

                default:
                    throw new NotSupportedException();
            }
        }

        public static FilterNamingConventionSnakeCase Default { get; } =
            new FilterNamingConventionSnakeCase();
    }
}
