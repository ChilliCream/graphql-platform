using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Properties.Resources;

namespace HotChocolate.Types.Spatial
{
    public sealed class GeoJsonMultiLineStringInput
        : GeoJsonInputObjectType<MultiLineString>
    {
        public GeoJsonMultiLineStringInput() : base(GeoJsonGeometryType.MultiLineString)
        {
        }

        protected override void Configure(IInputObjectTypeDescriptor<MultiLineString> descriptor)
        {
            descriptor.GeoJsonName(nameof(GeoJsonMultiLineStringInput));

            descriptor.BindFieldsExplicitly();

            descriptor.Field(TypeFieldName)
                .Type<GeoJsonGeometryEnumType>()
                .Description(GeoJson_Field_Type_Description);

            descriptor.Field(CoordinatesFieldName)
                .Type<ListType<ListType<GeoJsonPositionType>>>()
                .Description(GeoJson_Field_Coordinates_Description_MultiLineString);

            descriptor.Field(CrsFieldName)
                .Type<IntType>()
                .Description(GeoJson_Field_Crs_Description);
        }
    }
}
