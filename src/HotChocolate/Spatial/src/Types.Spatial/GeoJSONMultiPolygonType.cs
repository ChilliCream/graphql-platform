using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONMultiPolygonType : ObjectType<MultiPolygon>
    {
        protected override void Configure(IObjectTypeDescriptor<MultiPolygon> descriptor)
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
