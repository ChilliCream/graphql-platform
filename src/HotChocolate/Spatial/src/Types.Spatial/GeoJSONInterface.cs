namespace HotChocolate.Types.Spatial
{
    public class GeoJSONInterface : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name(nameof(GeoJSONInterface));

            // TODO: move to resource
            descriptor.Field("type")
                .Type<NonNullType<EnumType<GeoJSONGeometryType>>>()
                .Description("The geometry type of the GeoJSON object");

            descriptor.Field("bbox")
                .Type<ListType<FloatType>>()
                .Description("The minimum bounding box around the geometry object");

            descriptor.Field("crs")
                .Type<IntType>()
                .Description("The coordinate reference system integer identifier");
        }
    }
}
