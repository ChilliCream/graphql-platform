using HotChocolate.Language;
using HotChocolate.Types.Spatial.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial;

public sealed class GeometryType
    : ScalarType<Geometry>
    , IGeoJsonObjectType
    , IGeoJsonInputType
{
    public GeometryType(
        string name,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    [ActivatorUtilitiesConstructor]
    public GeometryType() : base(GeometryTypeName)
    {
    }

    public override object? Deserialize(object? resultValue)
        => GeoJsonGeometrySerializer.Default.Deserialize(this, resultValue);

    public override object? CoerceOutputValue(object? runtimeValue)
        => GeoJsonGeometrySerializer.Default.Serialize(this, runtimeValue);

    public override bool IsValueCompatible(IValueNode valueLiteral)
        => GeoJsonGeometrySerializer.Default.IsInstanceOfType(this, valueLiteral);

    public override bool IsInstanceOfType(object? runtimeValue)
        => GeoJsonGeometrySerializer.Default.IsInstanceOfType(this, runtimeValue);

    public override object? CoerceInputLiteral(IValueNode valueSyntax)
        => GeoJsonGeometrySerializer.Default.ParseLiteral(this, valueSyntax);

    public override IValueNode CoerceInputValue(object? runtimeValue)
        => GeoJsonGeometrySerializer.Default.ParseValue(this, runtimeValue);

    public override IValueNode ParseResult(object? resultValue)
        => GeoJsonGeometrySerializer.Default.ParseResult(this, resultValue);
}
