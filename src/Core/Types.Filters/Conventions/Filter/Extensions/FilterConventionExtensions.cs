using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters.Conventions
{
    public static class FilterConventionExtensions
    {
        public static IFilterConventionDescriptor UseDefault(
            this IFilterConventionDescriptor descriptor)
        {
            return descriptor.ArgumentName("where")
                .ArrayFilterPropertyName("element")
                .GetFilterTypeName(
                    (IDescriptorContext context, Type entityType) =>
                        context.Naming.GetTypeName(entityType, TypeKind.Object) + "Filter")
                .Type(FilterKind.Array)
                    .Operation(FilterOperationKind.ArrayAll).And()
                    .Operation(FilterOperationKind.ArrayAny).And()
                    .Operation(FilterOperationKind.ArraySome).And()
                    .Operation(FilterOperationKind.ArrayNone).And()
                    .And()
                .Type(FilterKind.Boolean)
                    .Operation(FilterOperationKind.Equals).And()
                    .Operation(FilterOperationKind.NotEquals).And()
                    .And()
                .Type(FilterKind.Comparable)
                    .Operation(FilterOperationKind.Equals).And()
                    .Operation(FilterOperationKind.NotEquals).And()
                    .Operation(FilterOperationKind.In).And()
                    .Operation(FilterOperationKind.NotIn).And()
                    .Operation(FilterOperationKind.GreaterThan).And()
                    .Operation(FilterOperationKind.NotGreaterThan).And()
                    .Operation(FilterOperationKind.GreaterThanOrEquals).And()
                    .Operation(FilterOperationKind.NotGreaterThanOrEquals).And()
                    .Operation(FilterOperationKind.LowerThan).And()
                    .Operation(FilterOperationKind.NotLowerThan).And()
                    .Operation(FilterOperationKind.LowerThanOrEquals).And()
                    .Operation(FilterOperationKind.NotLowerThanOrEquals).And()
                    .And()
                .Type(FilterKind.Object)
                    .Operation(FilterOperationKind.Equals).And()
                    .And()
                .Type(FilterKind.String)
                    .Operation(FilterOperationKind.Equals).And()
                    .Operation(FilterOperationKind.NotEquals).And()
                    .Operation(FilterOperationKind.Contains).And()
                    .Operation(FilterOperationKind.NotContains).And()
                    .Operation(FilterOperationKind.StartsWith).And()
                    .Operation(FilterOperationKind.NotStartsWith).And()
                    .Operation(FilterOperationKind.EndsWith).And()
                    .Operation(FilterOperationKind.NotEndsWith).And()
                    .Operation(FilterOperationKind.In).And()
                    .Operation(FilterOperationKind.NotIn).And()
                    .And()
                .UseSnakeCase();
        }

        public static IFilterConventionDescriptor UseSnakeCase(
        this IFilterConventionDescriptor descriptor)
        {
            return descriptor
                .Operation(FilterOperationKind.Equals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name)
                    .And()
                .Operation(FilterOperationKind.NotEquals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_not")
                    .And()
                .Operation(FilterOperationKind.Contains).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_contains")
                    .And()
                .Operation(FilterOperationKind.NotContains).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_not_contains")
                    .And()
                .Operation(FilterOperationKind.In).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_in")
                    .And()
                .Operation(FilterOperationKind.NotIn).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_not_in")
                    .And()
                .Operation(FilterOperationKind.StartsWith).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_starts_with")
                    .And()
                .Operation(FilterOperationKind.NotStartsWith).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_not_starts_with")
                    .And()
                .Operation(FilterOperationKind.EndsWith).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_ends_with")
                    .And()
                .Operation(FilterOperationKind.NotEndsWith).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_not_ends_with")
                    .And()
                .Operation(FilterOperationKind.GreaterThan).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_gt")
                    .And()
                .Operation(FilterOperationKind.NotGreaterThan).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_not_gt")
                    .And()
                .Operation(FilterOperationKind.GreaterThanOrEquals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_gte")
                    .And()
                .Operation(FilterOperationKind.NotGreaterThanOrEquals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_not_gte")
                    .And()
                .Operation(FilterOperationKind.LowerThan).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_lt")
                    .And()
                .Operation(FilterOperationKind.NotLowerThan).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_not_lt")
                    .And()
                .Operation(FilterOperationKind.LowerThanOrEquals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_lte")
                    .And()
                .Operation(FilterOperationKind.NotLowerThanOrEquals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_not_lte")
                    .And()
                .Operation(FilterOperationKind.Object).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name)
                    .And()
                .Operation(FilterOperationKind.ArraySome).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_some")
                    .And()
                .Operation(FilterOperationKind.ArrayNone).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_none")
                    .And()
                .Operation(FilterOperationKind.ArrayAll).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_all")
                    .And()
                .Operation(FilterOperationKind.ArrayAny).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) => d.Name + "_any")
                    .And();
        }

        public static IFilterConventionDescriptor UsePascalCase(
            this IFilterConventionDescriptor descriptor)
        {
            return descriptor.ArgumentName("Where")
                .Operation(FilterOperationKind.Equals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d))
                    .And()
                .Operation(FilterOperationKind.NotEquals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Not")
                    .And()
                .Operation(FilterOperationKind.Contains).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Contains")
                    .And()
                .Operation(FilterOperationKind.NotContains).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Not_Contains")
                    .And()
                .Operation(FilterOperationKind.In).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_In")
                    .And()
                .Operation(FilterOperationKind.NotIn).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Not_In")
                    .And()
                .Operation(FilterOperationKind.StartsWith).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_StartsWith")
                    .And()
                .Operation(FilterOperationKind.NotStartsWith).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Not_StartsWith")
                    .And()
                .Operation(FilterOperationKind.EndsWith).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_EndsWith")
                    .And()
                .Operation(FilterOperationKind.NotEndsWith).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Not_EndsWith")
                    .And()
                .Operation(FilterOperationKind.GreaterThan).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Gt")
                    .And()
                .Operation(FilterOperationKind.NotGreaterThan).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Not_Gt")
                    .And()
                .Operation(FilterOperationKind.GreaterThanOrEquals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Gte")
                    .And()
                .Operation(FilterOperationKind.NotGreaterThanOrEquals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Not_Gte")
                    .And()
                .Operation(FilterOperationKind.LowerThan).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Lt")
                    .And()
                .Operation(FilterOperationKind.NotLowerThan).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Not_Lt")
                    .And()
                .Operation(FilterOperationKind.LowerThanOrEquals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Lte")
                    .And()
                .Operation(FilterOperationKind.NotLowerThanOrEquals).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Not_Lte")
                    .And()
                .Operation(FilterOperationKind.Object).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d))
                    .And()
                .Operation(FilterOperationKind.ArraySome).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Some")
                    .And()
                .Operation(FilterOperationKind.ArrayNone).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_None")
                    .And()
                .Operation(FilterOperationKind.ArrayAll).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_All")
                    .And()
                .Operation(FilterOperationKind.ArrayAny).Name(
                    (FilterFieldDefintion d, FilterOperationKind k) =>
                        GetNameForDefintion(d) + "_Any")
                    .And();
        }

        private static string GetNameForDefintion(FilterFieldDefintion definition)
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
