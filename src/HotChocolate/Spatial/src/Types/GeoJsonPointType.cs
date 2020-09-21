using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.Properties.Resources;

namespace HotChocolate.Types.Spatial
{
    public sealed class GeoJsonPointType : ObjectType<Point>
    {
        protected override void Configure(IObjectTypeDescriptor<Point> descriptor)
        {
            descriptor.GeoJsonName(nameof(GeoJsonPointType));

            descriptor.BindFieldsExplicitly();

            descriptor.Implements<GeoJsonInterface>();

            descriptor
                .Field(x => x.Coordinates)
                .Description(GeoJson_Field_Coordinates_Description_Point);

            descriptor
                .Field<GeoJsonResolvers>(x => x.GetType(default!))
                .Description(GeoJson_Field_Type_Description);

            descriptor
                .Field<GeoJsonResolvers>(x => x.GetBbox(default!))
                .Description(GeoJson_Field_Bbox_Description);

            descriptor
                .Field<GeoJsonResolvers>(x => x.GetCrs(default!))
                .Description(GeoJson_Field_Crs_Description);
        }
    }
}
