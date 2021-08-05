using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Spatial.Serialization;

namespace HotChocolate.Types.Spatial
{
    public abstract class GeoJsonInputType<T>
        : InputObjectType<T>
        , IGeoJsonInputType
    {
        private readonly IGeoJsonSerializer _serializer;

        protected GeoJsonInputType(GeoJsonGeometryType geometryType)
        {
            _serializer = GeoJsonSerializers.Serializers[geometryType];
        }

        protected override InputObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        {
            InputObjectTypeDefinition definition = base.CreateDefinition(context);

            definition.CreateInstance = _serializer.CreateInstance;
            definition.GetFieldData = _serializer.GetFieldData;

            return definition;
        }
    }
}
