using HotChocolate.Data.Filters;
using NetTopologySuite.Geometries;
using static HotChocolate.Data.Spatial.Filters.SpatialFilterOperations;

namespace HotChocolate.Data.Spatial.Filters
{
    public class GeometryFilterType<T> : FilterInputType<T>
        where T : Geometry
    {
        protected override void Configure(IFilterInputTypeDescriptor<T> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(x => x.Area);
            descriptor.Field(x => x.Boundary);
            descriptor.Field(x => x.Centroid);
            descriptor.Field(x => x.Dimension);
            descriptor.Field(x => x.Envelope);
            descriptor.Field(x => x.GeometryType);
            descriptor.Field(x => x.InteriorPoint);
            descriptor.Field(x => x.IsSimple);
            descriptor.Field(x => x.IsValid);
            descriptor.Field(x => x.Length);
            descriptor.Field(x => x.NumPoints);
            descriptor.Field(x => x.OgcGeometryType);
            descriptor.Field(x => x.PointOnSurface);
            descriptor.Field(x => x.SRID);

            descriptor.Operation(Contains).Type<GeometryContainsOperationType>();
            descriptor.Operation(Distance).Type<GeometryDistanceOperationType>();
        }
    }
}
