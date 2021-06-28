using HotChocolate.Language;
using HotChocolate.Types.Spatial.Serialization;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial
{
    public sealed class GeometryType
        : ScalarType<Geometry>
        , IGeoJsonObjectType
        , IGeoJsonInputType
    {
        public GeometryType() : base(GeometryTypeName)
        {
        }

        public GeometryType(
            NameString name,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
        }

        public override object? Deserialize(object? resultValue)
        {
            try
            {
                return GeoJsonGeometrySerializer.Default.Deserialize(resultValue);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override object? Serialize(object? runtimeValue)
        {
            try
            {
                return GeoJsonGeometrySerializer.Default.Serialize(runtimeValue);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            try
            {
                return GeoJsonGeometrySerializer.Default.IsInstanceOfType(valueSyntax);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override bool IsInstanceOfType(object? runtimeValue)
        {
            try
            {
                return GeoJsonGeometrySerializer.Default.IsInstanceOfType(runtimeValue);
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
                return GeoJsonGeometrySerializer.Default.ParseLiteral(valueSyntax, withDefaults);
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
                return GeoJsonGeometrySerializer.Default.ParseValue(runtimeValue);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            try
            {
                return GeoJsonGeometrySerializer.Default.ParseResult(resultValue);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }
    }
}
