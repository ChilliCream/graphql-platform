using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Types.Spatial.GeoJsonGeometryType;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization;

internal class GeoJsonTypeSerializer : GeoJsonSerializerBase<GeoJsonGeometryType>
{
    private static readonly IDictionary<string, GeoJsonGeometryType> s_nameLookup =
        new Dictionary<string, GeoJsonGeometryType>
        {
            { nameof(Point), Point },
            { nameof(MultiPoint), MultiPoint },
            { nameof(LineString), LineString },
            { nameof(MultiLineString), MultiLineString },
            { nameof(Polygon), Polygon },
            { nameof(MultiPolygon), MultiPolygon },
            { nameof(GeometryCollection), GeometryCollection }
        };

    private static readonly IDictionary<GeoJsonGeometryType, string> s_valueLookup =
        new Dictionary<GeoJsonGeometryType, string>
        {
            { Point, nameof(Point) },
            { MultiPoint, nameof(MultiPoint) },
            { LineString, nameof(LineString) },
            { MultiLineString, nameof(MultiLineString) },
            { Polygon, nameof(Polygon) },
            { MultiPolygon, nameof(MultiPolygon) },
            { GeometryCollection, nameof(GeometryCollection) }
        };

    public override bool IsValueCompatible(IType type, IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(valueLiteral);

        if (valueLiteral is NullValueNode)
        {
            return true;
        }

        if (valueLiteral is EnumValueNode ev)
        {
            return s_nameLookup.ContainsKey(ev.Value);
        }

        if (valueLiteral is StringValueNode sv)
        {
            return s_nameLookup.ContainsKey(sv.Value);
        }

        return false;
    }

    public override bool IsValueCompatible(IType type, JsonElement inputValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (inputValue.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (inputValue.ValueKind == JsonValueKind.String)
        {
            var value = inputValue.GetString();
            return value is not null && s_nameLookup.ContainsKey(value);
        }

        return false;
    }

    public override object? CoerceInputLiteral(IType type, IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(valueLiteral);

        if (valueLiteral is EnumValueNode evn
            && s_nameLookup.TryGetValue(evn.Value, out var ev))
        {
            return ev;
        }

        if (valueLiteral is StringValueNode svn
            && s_nameLookup.TryGetValue(svn.Value, out ev))
        {
            return ev;
        }

        if (valueLiteral is NullValueNode)
        {
            return null;
        }

        throw Serializer_CouldNotParseLiteral(type);
    }

    public override object? CoerceInputValue(IType type, JsonElement inputValue, IFeatureProvider context)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (inputValue.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (inputValue.ValueKind == JsonValueKind.String)
        {
            var value = inputValue.GetString();
            if (value is not null && s_nameLookup.TryGetValue(value, out var geometryType))
            {
                return geometryType;
            }
        }

        throw Serializer_CouldNotParseLiteral(type);
    }

    public object? CoerceInputValueFromJson(IType type, JsonElement inputValue)
    {
        return CoerceInputValue(type, inputValue, null!);
    }

    public override void CoerceOutputValue(IType type, object runtimeValue, ResultElement resultValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is GeoJsonGeometryType geometryType
            && s_valueLookup.TryGetValue(geometryType, out var enumValue))
        {
            resultValue.SetStringValue(enumValue);
            return;
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override IValueNode ValueToLiteral(IType type, object? runtimeValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is GeoJsonGeometryType value
            && s_valueLookup.TryGetValue(value, out var enumValue))
        {
            return new EnumValueNode(enumValue);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override IValueNode CoordinateToLiteral(IType type, object? runtimeValue)
    {
        return ValueToLiteral(type, runtimeValue);
    }

    public override void CoerceOutputCoordinates(IType type, object runtimeValue, ResultElement resultElement)
    {
        throw new NotSupportedException("GeoJsonTypeSerializer does not support coordinate serialization");
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        throw new NotSupportedException("GeoJsonTypeSerializer does not support CreateInstance");
    }

    public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
    {
        throw new NotSupportedException("GeoJsonTypeSerializer does not support GetFieldData");
    }

    public bool TryParseString(string type, out GeoJsonGeometryType geometryType) =>
        s_nameLookup.TryGetValue(type, out geometryType);

    public static readonly GeoJsonTypeSerializer Default = new();
}
