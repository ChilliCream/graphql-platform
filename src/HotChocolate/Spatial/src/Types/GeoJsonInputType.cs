using System;
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

        protected override Func<object?[], object> OnCompleteCreateInstance(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition)
            => CreateInstance;

        private object CreateInstance(object?[] fieldValues)
        {
            try
            {
                return _serializer.CreateInstance(fieldValues);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        protected override Action<object, object?[]> OnCompleteGetFieldValues(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition)
            => GetFieldData;

        private void GetFieldData(object runtimeValue, object?[] fieldValues)
        {
            try
            {
                _serializer.GetFieldData(runtimeValue, fieldValues);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }
    }
}
