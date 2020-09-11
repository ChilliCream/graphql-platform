using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.Properties.Resources;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONPolygonType : ObjectType<Polygon>
    {
        protected override void Configure(IObjectTypeDescriptor<Polygon> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Implements<GeoJSONInterface>();

            descriptor.Field(x => x.Coordinates)
                .Description(GeoJSON_Field_Coordinates_Description_Polygon);
            descriptor.Field<GeoJSONResolvers>(x => x.GetType(default!))
                .Description(GeoJSON_Field_Type_Description);
            descriptor.Field<GeoJSONResolvers>(x => x.GetBbox(default!))
                .Description(GeoJSON_Field_Bbox_Description);
            descriptor.Field<GeoJSONResolvers>(x => x.GetCrs(default!))
                .Description(GeoJSON_Field_Crs_Description);
        }
    }
}
