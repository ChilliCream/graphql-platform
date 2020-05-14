using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.CRS
{
    public class GeoJSONMultiPolygonTypeExtension : ObjectTypeExtension<MultiPolygon>
    {
        protected override void Configure(IObjectTypeDescriptor<MultiPolygon> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field<GeoJSONResolvers>(x => x.GetCrs(default!));
        }
    }
}
