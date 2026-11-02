using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial;

public sealed class GeoJsonMultiLineStringType
    : ObjectType<MultiLineString>
    , IGeoJsonObjectType
{
    protected override void Configure(IObjectTypeDescriptor<MultiLineString> descriptor)
    {
        descriptor
            .Name(MultiLineStringTypeName)
            .Implements<GeoJsonInterfaceType>()
            .BindFieldsExplicitly();

        descriptor
            .Field<GeoJsonResolvers>(x => x.GetGeometryCollectionCoordinates(null!))
            .Name(CoordinatesFieldName)
            .Description(GeoJson_Field_Coordinates_Description_MultiLineString)
            .Type<ListType<ListType<GeoJsonPositionType>>>();

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
