using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types.Spatial.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial;

public sealed class GeometryType
    : ScalarType<Geometry, ObjectValueNode>
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

    protected override Geometry OnCoerceInputLiteral(ObjectValueNode valueLiteral)
        => (Geometry)GeoJsonGeometrySerializer.Default.CoerceInputLiteral(this, valueLiteral)!;

    protected override Geometry OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => (Geometry)GeoJsonGeometrySerializer.Default.CoerceInputValue(this, inputValue, context)!;

    protected override void OnCoerceOutputValue(Geometry runtimeValue, ResultElement resultValue)
        => GeoJsonGeometrySerializer.Default.CoerceOutputValue(this, runtimeValue, resultValue);

    protected override ObjectValueNode OnValueToLiteral(Geometry runtimeValue)
        => (ObjectValueNode)GeoJsonGeometrySerializer.Default.ValueToLiteral(this, runtimeValue);
}
