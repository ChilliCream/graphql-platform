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
            descriptor.AddImplicitFilter(TryCreateGeometryFiler, 0)
                .Type(SpatialFilterKind.Geometry)
                .Operation(SpatialFilterOperation.Distance)
                    .Name((def, _) => def.Name + "_distance")
                    .Description("")
                    .And()
                .And()
                .AddImplicitFilter(TryCreateGeometryFiler, 0)
                .Type(SpatialFilterKind.Geometry)
                .Operation(SpatialFilterOperation.Area)
                    .Name((def, _) => def.Name + "_area")
                    .Description("")
                    .And()
                .And()
                .AddImplicitFilter(TryCreateGeometryFiler, 0)
                .Type(SpatialFilterKind.Geometry)
                .Operation(SpatialFilterOperation.Intersects)
                    .Name((def, _) => def.Name + "_intersects")
                    .Description("")
                    .And()
                .And()
                .AddImplicitFilter(TryCreateGeometryFiler, 0)
                .Type(SpatialFilterKind.Geometry)
                .Operation(SpatialFilterOperation.Length)
                    .Name((def, _) => def.Name + "_length")
                    .Description("")
                    .And()
                .And()
                .AddImplicitFilter(TryCreateGeometryFiler, 0)
                .Type(SpatialFilterKind.Geometry)
                .Operation(SpatialFilterOperation.Within)
                    .Name((def, _) => def.Name + "_within")
                    .Description("")
                    .And()
                .And();

        public static IFilterConventionDescriptor SpatialFilterExpression(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.UseExpressionVisitor() 
                .Kind(SpatialFilterKind.Geometry)
                    .And()
                .Kind(SpatialFilterOperation.Distance)
                    .Enter(GemetryHandlers.Enter)
                    .Leave(GemetryHandlers.Leave)
                    .And()
                .Kind(SpatialFilterKind.Geometry)
                    .Enter(ObjectFieldHandler.Enter)
                    .Leave(ObjectFieldHandler.Leave)
                    .Operation(SpatialFilterOperation.Area)
                        .Handler(GeometryOperationHandlers.Area).And()
                    .Operation(SpatialFilterOperation.Intersects)
                        .Handler(GeometryOperationHandlers.Intersects).And()
                    .Operation(SpatialFilterOperation.Length)
                        .Handler(GeometryOperationHandlers.Length).And()
                    .Operation(SpatialFilterOperation.Within)
                        .Handler(GeometryOperationHandlers.Within).And()
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
