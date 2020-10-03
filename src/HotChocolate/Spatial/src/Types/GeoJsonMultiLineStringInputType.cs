using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial
{
    public sealed class GeoJsonMultiLineStringInputType : GeoJsonInputType<MultiLineString>
    {
        public GeoJsonMultiLineStringInputType() : base(GeoJsonGeometryType.MultiLineString)
        {
        }

        protected override void Configure(IInputObjectTypeDescriptor<MultiLineString> descriptor)
        {
            descriptor
                .Name(MultiLineStringInputName)
                .BindFieldsExplicitly();

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
