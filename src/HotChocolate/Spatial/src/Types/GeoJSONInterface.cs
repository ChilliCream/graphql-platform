using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONInterface : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name(nameof(GeoJSONInterface));

            descriptor.Field(TypeFieldName)
                .Type<NonNullType<EnumType<GeoJSONGeometryType>>>()
                .Description(GeoJSON_Field_Type_Description);

            descriptor.Field(BboxFieldName)
                .Type<ListType<FloatType>>()
                .Description(GeoJSON_Field_Bbox_Description);

            descriptor.Field(CrsFieldName)
                .Type<IntType>()
                .Description(GeoJSON_Field_Crs_Description);
        }
    }
}
