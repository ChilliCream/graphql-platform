using System;
using HotChocolate.Data.Filters;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial.Filters
{
    public static class SpatialFilterConventionDescriptorExtensions
    {
        /// the default names of the spatial filter operations
        public static IFilterConventionDescriptor AddSpatialOperations(
            this IFilterConventionDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Operation(SpatialFilterOperations.Contains).Name("contains");
            descriptor.Operation(SpatialFilterOperations.Distance).Name("distance");
            descriptor.Operation(SpatialFilterOperations.Buffer).Name("buffer");
            descriptor.Operation(SpatialFilterOperations.Geometry).Name("geometry");
            return descriptor;
        }

        /// the fields and operations available to each type
        public static IFilterConventionDescriptor BindSpatialTypes(
            this IFilterConventionDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor
                .BindRuntimeType<Geometry, GeometryFilterType>();
            descriptor
                .BindRuntimeType<LineString, LineStringFilterType>();
            descriptor
                .BindRuntimeType<Point, PointFilterType>();
            descriptor
                .BindRuntimeType<MultiPolygon, MultiPolygonFilterType>();


            return descriptor;
        }
    }
}
