using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.CRS
{
    public class GeoJSONPolygonTypeExtension : ObjectTypeExtension<Polygon>
    {
        protected override void Configure(IObjectTypeDescriptor<Polygon> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field<GeoJSONResolvers>(x => x.GetCrs(default!));
        }
    }
}
