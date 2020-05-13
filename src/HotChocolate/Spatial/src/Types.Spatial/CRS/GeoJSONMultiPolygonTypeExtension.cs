using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace Types.Spatial.CRS
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
