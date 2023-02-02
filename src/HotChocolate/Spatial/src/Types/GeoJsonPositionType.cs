using HotChocolate.Language;
using HotChocolate.Types.Spatial.Properties;
using HotChocolate.Types.Spatial.Serialization;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial;

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
        => GeoJsonPositionSerializer.Default.IsInstanceOfType(this, valueSyntax);

    public override object? ParseLiteral(IValueNode valueSyntax)
        => GeoJsonPositionSerializer.Default.ParseLiteral(this, valueSyntax);

    public override IValueNode ParseValue(object? value)
        => GeoJsonPositionSerializer.Default.ParseValue(this, value);

    public override IValueNode ParseResult(object? resultValue)
        => GeoJsonPositionSerializer.Default.ParseResult(this, resultValue);

    public override bool TryDeserialize(object? serialized, out object? value)
        => GeoJsonPositionSerializer.Default.TryDeserialize(this, serialized, out value);

    public override bool TrySerialize(object? value, out object? serialized)
        => GeoJsonPositionSerializer.Default.TrySerialize(this, value, out serialized);
}
