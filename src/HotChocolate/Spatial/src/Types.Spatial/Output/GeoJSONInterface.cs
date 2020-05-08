using System;
using HotChocolate.Types;
using Types.Spatial.Common;

namespace Types.Spatial.Output
{
    public class GeoJSONInterfaceCrsExtension
        : InterfaceTypeExtension
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name(nameof(GeoJSONInterface));

            descriptor.Field("crs")
                .Type<StringType>();
        }
    }

    public class GeoJSONInterface : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name(nameof(GeoJSONInterface));

            descriptor.Field("type")
                .Type<NonNullType<EnumType<GeoJSONGeometryType>>>()
                .Description("Type of the GeoJSON Object");

            descriptor.Field("bbox")
                .Type<ListType<FloatType>>();
        }
    }
}
