using System;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Filters.Spatial
{
    public static class SpatialFilterConventionDescriptorExtensions
    {
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
                .BindRuntimeType<Geometry, GeometryFilterType>()
                .BindRuntimeType<Point, PointFilterType>()
                .BindRuntimeType<MultiPoint, MultiPointFilterType>()
                .BindRuntimeType<LineString, LineStringFilterType>()
                .BindRuntimeType<MultiLineString, MultiLineStringFilterType>()
                .BindRuntimeType<Polygon, PolygonFilterType>()
                .BindRuntimeType<MultiPolygon, MultiPolygonFilterType>();

            return descriptor;
        }
    }
}
