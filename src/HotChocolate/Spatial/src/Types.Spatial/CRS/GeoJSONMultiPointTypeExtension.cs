using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.CRS
{
    public class GeoJSONMultiPointTypeExtension : ObjectTypeExtension<MultiPoint>
    {
        protected override void Configure(IObjectTypeDescriptor<MultiPoint> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field<GeoJSONResolvers>(x => x.GetCrs(default!));
        }
    }
}
