using HotChocolate.Types;

namespace HotChocolate.Types.Spatial.CRS
{
    public class GeoJSONInterfaceCrsExtension : InterfaceTypeExtension
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name(nameof(GeoJSONInterface));
            descriptor.Field("crs").Type<GeoJSONCoordinateReferenceSystemType>();
        }
    }
}
