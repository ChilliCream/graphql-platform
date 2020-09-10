using System.Collections.Generic;
using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace HotChocolate.Spatial.Types
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
