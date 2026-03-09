using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types.Spatial.Properties;
using HotChocolate.Types.Spatial.Serialization;

namespace HotChocolate.Types.Spatial;

/// <summary>
/// <para>
/// The Coordinates scalar type represents a list of arbitrary depth of positions
/// </para>
/// <para>https://tools.ietf.org/html/rfc7946#section-3.1.1</para>
/// </summary>
public sealed class GeoJsonCoordinatesType : ScalarType<object[], ListValueNode>
{
    public GeoJsonCoordinatesType() : base(WellKnownTypeNames.CoordinatesTypeName)
    {
        Description = Resources.GeoJsonCoordinatesScalar_Description;
    }

    protected override object[] OnCoerceInputLiteral(ListValueNode valueLiteral)
        => (object[])GeoJsonCoordinatesSerializer.Default.CoerceInputLiteral(this, valueLiteral)!;

    protected override object[] OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => (object[])GeoJsonCoordinatesSerializer.Default.CoerceInputValue(this, inputValue, context)!;

    protected override void OnCoerceOutputValue(object[] runtimeValue, ResultElement resultValue)
        => GeoJsonCoordinatesSerializer.Default.CoerceOutputCoordinates(this, runtimeValue, resultValue);

    protected override ListValueNode OnValueToLiteral(object[] runtimeValue)
        => (ListValueNode)GeoJsonCoordinatesSerializer.Default.ValueToLiteral(this, runtimeValue);
}
