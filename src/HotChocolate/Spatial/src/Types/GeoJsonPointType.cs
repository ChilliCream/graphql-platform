using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial;

public sealed class GeoJsonPointType
    : ObjectType<Point>
    , IGeoJsonObjectType
{
    protected override void Configure(IObjectTypeDescriptor<Point> descriptor)
    {
        descriptor
            .Name(PointTypeName)
            .Implements<GeoJsonInterfaceType>()
            .BindFieldsExplicitly();

        descriptor
            .Field(x => x.Coordinate)
            .Name(CoordinatesFieldName)
            .Description(GeoJson_Field_Coordinates_Description_Point);

        descriptor
            .Field<GeoJsonResolvers>(x => x.GetType(null!))
            .Description(GeoJson_Field_Type_Description);

        descriptor
            .Field<GeoJsonResolvers>(x => x.GetBbox(null!))
            .Description(GeoJson_Field_Bbox_Description);

        descriptor
            .Field<GeoJsonResolvers>(x => x.GetCrs(null!))
            .Description(GeoJson_Field_Crs_Description);
    }
}
