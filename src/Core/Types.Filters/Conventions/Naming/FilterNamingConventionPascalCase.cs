using System;

namespace HotChocolate.Types.Filters
{
    public class FilterNamingConventionPascalCase : IFilterNamingConvention
    {
        public NameString CreateFieldName(FilterFieldDefintion definition, FilterOperationKind kind)
        {
            switch (kind)
            {
                case FilterOperationKind.Equals:
                    return GetNameForDefintion(definition);
                case FilterOperationKind.NotEquals:
                    return GetNameForDefintion(definition) + "_Not";

                case FilterOperationKind.Contains:
                    return GetNameForDefintion(definition) + "_Contains";
                case FilterOperationKind.NotContains:
                    return GetNameForDefintion(definition) + "_Not_Contains";

                case FilterOperationKind.In:
                    return GetNameForDefintion(definition) + "_In";
                case FilterOperationKind.NotIn:
                    return GetNameForDefintion(definition) + "_Not_In";

                case FilterOperationKind.StartsWith:
                    return GetNameForDefintion(definition) + "_StartsWith";
                case FilterOperationKind.NotStartsWith:
                    return GetNameForDefintion(definition) + "_Not_StartsWith";

                case FilterOperationKind.EndsWith:
                    return GetNameForDefintion(definition) + "_EndsWith";
                case FilterOperationKind.NotEndsWith:
                    return GetNameForDefintion(definition) + "_Not_EndsWith";

                case FilterOperationKind.GreaterThan:
                    return GetNameForDefintion(definition) + "_Gt";
                case FilterOperationKind.NotGreaterThan:
                    return GetNameForDefintion(definition) + "_Not_Gt";

                case FilterOperationKind.GreaterThanOrEquals:
                    return GetNameForDefintion(definition) + "_Gte";
                case FilterOperationKind.NotGreaterThanOrEquals:
                    return GetNameForDefintion(definition) + "_Not_Gte";

                case FilterOperationKind.LowerThan:
                    return GetNameForDefintion(definition) + "_Lt";
                case FilterOperationKind.NotLowerThan:
                    return GetNameForDefintion(definition) + "_Not_Lt";

                case FilterOperationKind.LowerThanOrEquals:
                    return GetNameForDefintion(definition) + "_Lte";
                case FilterOperationKind.NotLowerThanOrEquals:
                    return GetNameForDefintion(definition) + "_Not_Lte";

                case FilterOperationKind.Object:
                    return GetNameForDefintion(definition);

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

        private string GetNameForDefintion(FilterFieldDefintion definition)
        {
            var name = definition.Name.Value;
            if (name.Length > 1)
            {
                return name.Substring(0, 1).ToUpperInvariant() +
                    name.Substring(1);
            }
            return name.ToUpperInvariant();
        }
    }
}
