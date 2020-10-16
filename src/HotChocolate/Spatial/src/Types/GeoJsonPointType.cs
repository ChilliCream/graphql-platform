using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial
{
    public sealed class GeoJsonPointType : ObjectType<Point>
    {
        protected override void Configure(IObjectTypeDescriptor<Point> descriptor)
        {
            descriptor
                .Name(PointTypeName)
                .Implements<GeoJsonInterfaceType>()
                .BindFieldsExplicitly();

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
