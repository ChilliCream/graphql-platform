
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Expressions;

namespace Filtering.Customization
{
    public static class DateTimeFilterConventionExtensions
    {
        public static IFilterConventionDescriptor UseDateTimeFilter(
            this IFilterConventionDescriptor descriptor) =>
                descriptor.UseDateTimeFilterImplicitly()
                    .UseDateTimeFilterExpression();

        public static IFilterConventionDescriptor UseDateTimeFilterImplicitly(
            this IFilterConventionDescriptor descriptor) =>
                descriptor.AddImplicitFilter(TryCreateDateTimeFiler)
                    .Type(FilterKind.DateTime)
                    .Operation(FilterOperationKind.GreaterThanOrEquals)
                        .Name((def, _) => def.Name + "_from")
                        .Description("")
                        .And()
                    .Operation(FilterOperationKind.LowerThanOrEquals)
                        .Name((def, _) => def.Name + "_to")
                        .Description("")
                        .And()
                    .And();

        public static IFilterConventionDescriptor UseDateTimeFilterExpression(
            this IFilterConventionDescriptor descriptor) =>
                descriptor.UseExpressionVisitor()
                    .Kind(FilterKind.DateTime)
                        .Operation(FilterOperationKind.LowerThanOrEquals)
                            .Handler(ComparableOperationHandlers.LowerThanOrEquals).And()
                        .Operation(FilterOperationKind.GreaterThanOrEquals)
                        .Handler(ComparableOperationHandlers.GreaterThanOrEquals).And()
                        .And()
                    .And();

        private static bool TryCreateDateTimeFiler(
            IDescriptorContext context,
            Type type,
            PropertyInfo property,
            IFilterConvention filterConventions,
            [NotNullWhen(true)] out FilterFieldDefintion? definition)
        {
            if (type == typeof(DateTime))
            {
                var field = new DateTimeFilterFieldDescriptor(
                    context, property, filterConventions);
                definition = field.CreateDefinition();
                return true;
            }

            definition = null;
            return false;
        }


    }
}