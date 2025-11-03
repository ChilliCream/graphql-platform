using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial;

public sealed class GeoJsonMultiPointType
    : ObjectType<MultiPoint>
    , IGeoJsonObjectType
{
    protected override void Configure(IObjectTypeDescriptor<MultiPoint> descriptor)
    {
        descriptor
            .Name(MultiPointTypeName)
            .Implements<GeoJsonInterfaceType>()
            .BindFieldsExplicitly();

        descriptor
            .Field(x => x.Coordinates)
            .Description(GeoJson_Field_Coordinates_Description_MultiPoint);

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
