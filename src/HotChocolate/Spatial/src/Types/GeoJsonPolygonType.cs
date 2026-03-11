using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial;

public sealed class GeoJsonPolygonType
    : ObjectType<Polygon>
    , IGeoJsonObjectType
{
    protected override void Configure(IObjectTypeDescriptor<Polygon> descriptor)
    {
        descriptor
            .Name(PolygonTypeName)
            .Implements<GeoJsonInterfaceType>()
            .BindFieldsExplicitly();

        descriptor
            .Field<Resolvers>(x => x.GetCoordinates(null!))
            .Name(WellKnownFields.CoordinatesFieldName)
            .Description(GeoJson_Field_Coordinates_Description_Polygon)
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

    public class Resolvers
    {
        public Coordinate[][] GetCoordinates([Parent] Polygon polygon)
        {
            var coordinates = new Coordinate[polygon.NumInteriorRings + 1][];
            coordinates[0] = polygon.ExteriorRing.Coordinates;

            for (var i = 0; i < polygon.InteriorRings.Length; i++)
            {
                coordinates[i] = polygon.InteriorRings[i].Coordinates;
            }

            return coordinates;
        }
    }
}
