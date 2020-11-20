using System;
using HotChocolate.Data.Filters.Expressions;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Filters.Spatial
{
    public static class SpatialFilterConventionDescriptorExtensions
    {
        /// <summary>
        /// Adds the spatial filter defaults
        /// </summary>
        public static IFilterConventionDescriptor AddSpatialDefaults(
            this IFilterConventionDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.AddSpatialOperations();
            descriptor.BindSpatialTypes();
            descriptor.AddProviderExtension(
                new QueryableFilterProviderExtension(p => p.AddSpatialHandlers()));

            return descriptor;
        }

        /// <summary>
        /// The default names of the spatial filter operations
        /// </summary>
        public static IFilterConventionDescriptor AddSpatialOperations(
            this IFilterConventionDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Operation(SpatialFilterOperations.Contains).Name("contains");
            descriptor.Operation(SpatialFilterOperations.Distance).Name("distance");
            descriptor.Operation(SpatialFilterOperations.Intersects).Name("intersects");
            descriptor.Operation(SpatialFilterOperations.Overlaps).Name("overlaps");
            descriptor.Operation(SpatialFilterOperations.Touches).Name("touches");
            descriptor.Operation(SpatialFilterOperations.Within).Name("within");
            descriptor.Operation(SpatialFilterOperations.Buffer).Name("buffer");
            descriptor.Operation(SpatialFilterOperations.Geometry).Name("geometry");

            return descriptor;
        }

        /// <summary>
        /// The fields and operations available to each type
        /// </summary>
        public static IFilterConventionDescriptor BindSpatialTypes(
            this IFilterConventionDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor
                .BindRuntimeType<Geometry, GeometryFilterInput>()
                .BindRuntimeType<Point, PointFilterInput>()
                .BindRuntimeType<MultiPoint, MultiPointFilterInput>()
                .BindRuntimeType<LineString, LineStringFilterInput>()
                .BindRuntimeType<MultiLineString, MultiLineStringFilterInput>()
                .BindRuntimeType<Polygon, PolygonFilterInput>()
                .BindRuntimeType<MultiPolygon, MultiPolygonFilterInput>();

            return descriptor;
        }
    }
}
