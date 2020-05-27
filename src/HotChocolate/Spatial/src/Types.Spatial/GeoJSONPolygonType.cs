using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONPolygonType : ObjectType<Polygon>
    {
        protected override void Configure(IObjectTypeDescriptor<Polygon> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Implements<GeoJSONInterface>();

            descriptor.Field(x => x.Coordinates);
            descriptor.Field<GeoJSONResolvers>(x => x.GetType(default!));
            descriptor.Field<GeoJSONResolvers>(x => x.GetBbox(default!));
            descriptor.Field<GeoJSONResolvers>(x => x.GetCrs(default!));
        }
    }
}
