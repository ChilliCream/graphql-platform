using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Expressions;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Filters.Expressions
{
    public static class FilterVisitorExtensions
    {
        public static IFilterConventionDescriptor UseSpatialFilters(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.SpatialFilterImplicitly().SpatialFilterExpression();

        public static IFilterConventionDescriptor SpatialFilterImplicitly(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.AddImplicitFilter(TryCreateGeometryFiler)
                .Type(SpatialFilterKind.Geometry)
                .Operation(SpatialFilterOperation.Distance)
                    .Name((def, _) => def.Name + "_distance")
                    .Description("")
                    .And()
                .And();

        public static IFilterConventionDescriptor SpatialFilterExpression(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.UseExpressionVisitor()
                .Kind(SpatialFilterKind.Geometry)
                    .Operation(SpatialFilterOperation.Distance)
                        .Handler(GemetryOperationHandlers.Distance).And()
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
