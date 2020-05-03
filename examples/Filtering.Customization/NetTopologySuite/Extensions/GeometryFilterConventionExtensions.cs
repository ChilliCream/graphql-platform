
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Expressions;
using NetTopologySuite.Geometries;

namespace Filtering.Customization
{
    public static class GeometryFilterConventionExtensions
    {
        public static IFilterConventionDescriptor UseGeometryFilter(
            this IFilterConventionDescriptor descriptor) =>
                descriptor.UseGeometryFilterImplicitly()
                    .UseGeometryFilterExpression();

        public static IFilterConventionDescriptor UseGeometryFilterImplicitly(
            this IFilterConventionDescriptor descriptor) =>
                descriptor.AddImplicitFilter(TryCreateGeometryFiler)
                    .Operation(FilterOperationKind.Skip)
                        .Name((def, _) => def.Name )
                        .Description("")
                    .And()
                    .Type(FilterKind.Geometry)
                    .Operation(FilterOperationKind.Distance)
                        .Name((def, _) => def.Name + "_distance")
                        .Description("")
                        .And()
                    .And();

        public static IFilterConventionDescriptor UseGeometryFilterExpression(
            this IFilterConventionDescriptor descriptor) =>
                descriptor.UseExpressionVisitor()
                    .Kind(FilterKind.Geometry)
                        .Operation(FilterOperationKind.Distance)
                            .Handler(ComparableOperationHandlers.LowerThanOrEquals).And()
                        .And()
                    .And();

        private static bool TryCreateGeometryFiler(
            IDescriptorContext context,
            Type type,
            PropertyInfo property,
            IFilterConvention filterConventions,
            [NotNullWhen(true)] out FilterFieldDefintion? definition)
        {
            if (typeof(Geometry).IsAssignableFrom(type))
            {
                var field = new GeometryFilterFieldDescriptor(
                    context, property, filterConventions);
                definition = field.CreateDefinition();
                return true;
            }

            definition = null;
            return false;
        }
    }
}