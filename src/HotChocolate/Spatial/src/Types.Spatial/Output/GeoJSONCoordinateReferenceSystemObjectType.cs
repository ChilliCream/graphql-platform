using HotChocolate.Types;

namespace Types.Spatial.Output
{
    public class GeoJSONCoordinateReferenceSystemObjectType : ObjectType<GeoJSONCoordinateReferenceSystem>
    {
        protected override void Configure(IObjectTypeDescriptor<GeoJSONCoordinateReferenceSystem> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(x => x.Type);
        }
    }
}
