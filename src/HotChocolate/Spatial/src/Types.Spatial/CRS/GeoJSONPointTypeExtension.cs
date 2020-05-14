using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.CRS
{
    public class GeoJSONPointTypeExtension : ObjectTypeExtension<Point>
    {
        protected override void Configure(IObjectTypeDescriptor<Point> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field<GeoJSONResolvers>(x => x.GetCrs(default!));
        }
    }
}
