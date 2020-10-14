using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial
{
    public sealed class GeoJsonLineStringInputType : GeoJsonInputType<LineString>
    {
        public GeoJsonLineStringInputType() : base(GeoJsonGeometryType.LineString)
        {
        }

        protected override void Configure(IInputObjectTypeDescriptor<LineString> descriptor)
        {
            descriptor
                .Name(LineStringInputName)
                .BindFieldsExplicitly();

            descriptor
                .Field(TypeFieldName)
                .Type<GeoJsonGeometryEnumType>()
                .Description(GeoJson_Field_Type_Description);

            descriptor
                .Field(CoordinatesFieldName)
                .Type<ListType<GeoJsonPositionType>>()
                .Description(GeoJson_Field_Coordinates_Description_LineString);

            descriptor
                .Field(CrsFieldName)
                .Type<IntType>()
                .Description(GeoJson_Field_Crs_Description);
        }
    }
}
