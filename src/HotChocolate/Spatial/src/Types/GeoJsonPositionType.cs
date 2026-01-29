using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
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
public sealed class GeoJsonPositionType : ScalarType<Coordinate, ListValueNode>
{
    public GeoJsonPositionType() : base(PositionTypeName)
    {
        Description = Resources.GeoJsonPositionScalar_Description;
    }

    protected override Coordinate OnCoerceInputLiteral(ListValueNode valueLiteral)
        => (Coordinate)GeoJsonPositionSerializer.Default.CoerceInputLiteral(this, valueLiteral)!;

    protected override Coordinate OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => (Coordinate)GeoJsonPositionSerializer.Default.CoerceInputValue(this, inputValue, context)!;

    protected override void OnCoerceOutputValue(Coordinate runtimeValue, ResultElement resultValue)
        => GeoJsonPositionSerializer.Default.CoerceOutputCoordinates(this, runtimeValue, resultValue);

    protected override ListValueNode OnValueToLiteral(Coordinate runtimeValue)
        => (ListValueNode)GeoJsonPositionSerializer.Default.ValueToLiteral(this, runtimeValue);
}
