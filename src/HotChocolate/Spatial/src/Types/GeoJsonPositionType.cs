using System;
using System.Collections;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using HotChocolate.Types.Spatial.Properties;

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
    public class GeoJsonPositionType : ScalarType<Coordinate>
    {
        public GeoJsonPositionType() : base("Position")
        {
            Description = Resources.GeoJsonPositionScalar_Description;
        }

        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            return GeoJsonPositionSerializer.Default.IsInstanceOfType(valueSyntax);
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {

            return GeoJsonPositionSerializer.Default.ParseLiteral(valueSyntax, withDefaults);
        }

        public override IValueNode ParseValue(object? value)
        {
            return GeoJsonPositionSerializer.Default.ParseValue(value);
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            return GeoJsonPositionSerializer.Default.ParseResult(resultValue);
        }

        public override bool TryDeserialize(object? serialized, out object? value)
        {
            return GeoJsonPositionSerializer.Default.TryDeserialize(serialized, out value);
        }

        public override bool TrySerialize(object? value, out object? serialized)
        {
            return GeoJsonPositionSerializer.Default.TrySerialize(value, out serialized);
        }
    }
}
