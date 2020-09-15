using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial
{
    public class GeoJsonInterface : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.GeoJsonName(nameof(GeoJsonInterface));

            descriptor.Field(TypeFieldName)
                .Type<NonNullType<EnumType<GeoJsonGeometryType>>>()
                .Description(GeoJson_Field_Type_Description);

            descriptor.Field(BboxFieldName)
                .Type<ListType<FloatType>>()
                .Description(GeoJson_Field_Bbox_Description);

            descriptor.Field(CrsFieldName)
                .Type<IntType>()
                .Description(GeoJson_Field_Crs_Description);
        }
    }
}
