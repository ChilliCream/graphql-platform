using HotChocolate.Language;
using HotChocolate.Types.Spatial.Properties;

namespace HotChocolate.Types.Spatial;

/// <summary>
/// <para>
/// The Coordinates scalar type represents a list of arbitrary depth of positions
/// </para>
/// <para>https://tools.ietf.org/html/rfc7946#section-3.1.1</para>
/// </summary>
public sealed class GeoJsonCoordinatesType : ScalarType<object[]>
{
    public GeoJsonCoordinatesType() : base(WellKnownTypeNames.CoordinatesTypeName)
    {
        Description = Resources.GeoJsonCoordinatesScalar_Description;
    }

    public override bool IsInstanceOfType(IValueNode valueSyntax)
        => GeoJsonCoordinatesSerializer.Default.IsInstanceOfType(this, valueSyntax);

    public override object? ParseLiteral(IValueNode valueSyntax)
        => GeoJsonCoordinatesSerializer.Default.ParseLiteral(this, valueSyntax);

    public override IValueNode ParseValue(object? value)
        => GeoJsonCoordinatesSerializer.Default.ParseValue(this, value);

    public override IValueNode ParseResult(object? resultValue)
        => GeoJsonCoordinatesSerializer.Default.ParseResult(this, resultValue);

    public override bool TryDeserialize(object? serialized, out object? value)
        => GeoJsonCoordinatesSerializer.Default.TryDeserialize(this, serialized, out value);

    public override bool TrySerialize(object? value, out object? serialized)
        => GeoJsonCoordinatesSerializer.Default.TrySerialize(this, value, out serialized);
}
