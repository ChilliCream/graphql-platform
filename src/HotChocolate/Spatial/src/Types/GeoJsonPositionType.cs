using HotChocolate.Language;
using NetTopologySuite.Geometries;
using HotChocolate.Types.Spatial.Properties;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial
{
    /// <summary>
    /// <para>
    /// The Position scalar type represents the coordinates of a <see cref="GeoJsonGeometryType"/>
    /// The implementation of this follows the specifications designed in IETF RFC 7946s
    /// Section 3.1.1
    /// </para>
    /// <para>https://tools.ietf.org/html/rfc7946#section-3.1.1</para>
    /// </summary>
    public sealed class GeoJsonPositionType : ScalarType<Coordinate>
    {
        public GeoJsonPositionType() : base(PositionTypeName)
        {
            Description = Resources.GeoJsonPositionScalar_Description;
        }

        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            try
            {
                return GeoJsonPositionSerializer.Default.IsInstanceOfType(valueSyntax);
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
                return GeoJsonPositionSerializer.Default.ParseLiteral(valueSyntax, withDefaults);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override IValueNode ParseValue(object? value)
        {
            try
            {
                return GeoJsonPositionSerializer.Default.ParseValue(value);
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
                return GeoJsonPositionSerializer.Default.ParseResult(resultValue);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override bool TryDeserialize(object? serialized, out object? value)
        {
            try
            {
                return GeoJsonPositionSerializer.Default.TryDeserialize(serialized, out value);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }

        public override bool TrySerialize(object? value, out object? serialized)
        {
            try
            {
                return GeoJsonPositionSerializer.Default.TrySerialize(value, out serialized);
            }
            catch (GeoJsonSerializationException ex)
            {
                throw ex.ToSerializationException(this);
            }
        }
    }
}
