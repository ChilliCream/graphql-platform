using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace Types.Spatial.CRS
{
    public class GeoJSONMultiLineStringTypeExtension : ObjectTypeExtension<MultiLineString>
    {
        protected override void Configure(IObjectTypeDescriptor<MultiLineString> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field<GeoJSONResolvers>(x => x.GetCrs(default!));
        }
    }
}
