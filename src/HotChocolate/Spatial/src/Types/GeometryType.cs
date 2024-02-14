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
    [ActivatorUtilitiesConstructor]
    public GeometryType() : base(GeometryTypeName)
    {
    }

    public GeometryType(
        string name,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    public override object? Deserialize(object? resultValue)
        => GeoJsonGeometrySerializer.Default.Deserialize(this, resultValue);

    public override object? Serialize(object? runtimeValue)
        => GeoJsonGeometrySerializer.Default.Serialize(this, runtimeValue);

    public override bool IsInstanceOfType(IValueNode valueSyntax)
        => GeoJsonGeometrySerializer.Default.IsInstanceOfType(this, valueSyntax);

    public override bool IsInstanceOfType(object? runtimeValue)
        => GeoJsonGeometrySerializer.Default.IsInstanceOfType(this, runtimeValue);

    public override object? ParseLiteral(IValueNode valueSyntax)
        => GeoJsonGeometrySerializer.Default.ParseLiteral(this, valueSyntax);

    public override IValueNode ParseValue(object? runtimeValue)
        => GeoJsonGeometrySerializer.Default.ParseValue(this, runtimeValue);

    public override IValueNode ParseResult(object? resultValue)
        => GeoJsonGeometrySerializer.Default.ParseResult(this, resultValue);
}
