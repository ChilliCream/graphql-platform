using HotChocolate.Language;
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

        public override bool IsInstanceOfType(IValueNode literal)
        {
            try
            {
                return _serializer.IsInstanceOfType(literal);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override bool IsInstanceOfType(object? value)
        {
            try
            {
                return _serializer.IsInstanceOfType(value);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            try
            {
                return _serializer.ParseLiteral(valueSyntax, withDefaults);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override IValueNode ParseValue(object? runtimeValue)
        {
            try
            {
                return _serializer.ParseValue(runtimeValue);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            try
            {
                return _serializer.TryDeserialize(resultValue, out runtimeValue);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            try
            {
                return _serializer.TrySerialize(runtimeValue, out resultValue);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }
    }
}
