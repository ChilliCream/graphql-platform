using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial
{
    public sealed class GeoJsonInterfaceType : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name(InterfaceTypeName);

            descriptor
                .Field(TypeFieldName)
                .Type<NonNullType<GeoJsonGeometryEnumType>>()
                .Description(GeoJson_Field_Type_Description);

            descriptor
                .Field(BboxFieldName)
                .Type<ListType<FloatType>>()
                .Description(GeoJson_Field_Bbox_Description);

            descriptor
                .Field(CrsFieldName)
                .Type<IntType>()
                .Description(GeoJson_Field_Crs_Description);
        }
    }
}
