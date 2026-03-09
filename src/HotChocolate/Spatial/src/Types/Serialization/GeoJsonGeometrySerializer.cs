using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.Serialization.GeoJsonSerializers;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Serialization;

internal sealed class GeoJsonGeometrySerializer : IGeoJsonSerializer
{
    public bool IsValueCompatible(IType type, IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(valueLiteral);

        if (valueLiteral is NullValueNode)
        {
            return true;
        }

        if (valueLiteral.Kind != SyntaxKind.ObjectValue)
        {
            return false;
        }

        return GetGeometrySerializer(type, (ObjectValueNode)valueLiteral)
            .IsValueCompatible(type, valueLiteral);
    }

    public bool IsValueCompatible(IType type, JsonElement inputValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (inputValue.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (inputValue.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!TryGetGeometryKindFromJson(type, inputValue, out var geometryType))
        {
            return false;
        }

        if (!Serializers.TryGetValue(geometryType, out var serializer))
        {
            return false;
        }

        return serializer.IsValueCompatible(type, inputValue);
    }

    public object? CoerceInputLiteral(IType type, IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (valueLiteral.Kind is SyntaxKind.NullValue)
        {
            return null;
        }

        if (valueLiteral.Kind is not SyntaxKind.ObjectValue)
        {
            throw Serializer_Parse_ValueKindInvalid(type, valueLiteral.Kind);
        }

        return GetGeometrySerializer(type, (ObjectValueNode)valueLiteral)
            .CoerceInputLiteral(type, valueLiteral);
    }

    public object? CoerceInputValue(IType type, JsonElement inputValue, IFeatureProvider context)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (inputValue.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (inputValue.ValueKind != JsonValueKind.Object)
        {
            throw Serializer_Parse_ValueKindInvalid(type, SyntaxKind.ObjectValue);
        }

        if (!TryGetGeometryKindFromJson(type, inputValue, out var geometryType))
        {
            throw Geometry_Parse_InvalidType(type);
        }

        if (!Serializers.TryGetValue(geometryType, out var serializer))
        {
            throw Geometry_Serializer_NotFound(type, geometryType);
        }

        return serializer.CoerceInputValue(type, inputValue, context);
    }

    public void CoerceOutputValue(IType type, object runtimeValue, ResultElement resultValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is not Geometry geometry)
        {
            throw Geometry_Serialize_InvalidGeometryType(type, runtimeValue.GetType());
        }

        if (!TryGetGeometryKind(geometry, out var kind))
        {
            throw Geometry_Serialize_TypeIsUnknown(type, geometry.GeometryType);
        }

        Serializers[kind].CoerceOutputValue(type, runtimeValue, resultValue);
    }

    public IValueNode ValueToLiteral(IType type, object? runtimeValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is not Geometry geometry)
        {
            throw Geometry_Parse_InvalidGeometryType(type, runtimeValue.GetType());
        }

        if (!TryGetGeometryKind(geometry, out var kind))
        {
            throw Geometry_Parse_TypeIsUnknown(type, geometry.GeometryType);
        }

        if (!Serializers.TryGetValue(kind, out var geometryType))
        {
            throw Geometry_Serializer_NotFound(type, kind);
        }

        return geometryType.ValueToLiteral(type, runtimeValue);
    }

    public IValueNode CoordinateToLiteral(IType type, object? runtimeValue)
        => throw Serializer_OperationIsNotSupported(type,
            this,
            nameof(CoordinateToLiteral));

    public void CoerceOutputCoordinates(IType type, object runtimeValue, ResultElement resultElement)
        => throw Serializer_OperationIsNotSupported(type,
            this,
            nameof(CoerceOutputCoordinates));

    public object CreateInstance(IType type, object?[] fieldValues)
        => throw Serializer_OperationIsNotSupported(type,
            this,
            nameof(CreateInstance));

    public void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
        => throw Serializer_OperationIsNotSupported(type,
            this,
            nameof(GetFieldData));

    private IGeoJsonSerializer GetGeometrySerializer(
        IType type,
        ObjectValueNode objectSyntax)
    {
        if (!TryGetGeometryKind(type, objectSyntax, out var geometryType))
        {
            throw Geometry_Parse_InvalidType(type);
        }

        if (!Serializers.TryGetValue(geometryType, out var serializer))
        {
            throw Geometry_Serializer_NotFound(type, geometryType);
        }

        return serializer;
    }

    private bool TryGetGeometryKind(
        Geometry geometry,
        out GeoJsonGeometryType geometryType) =>
        GeoJsonTypeSerializer.Default.TryParseString(geometry.GeometryType, out geometryType);

    private bool TryGetGeometryKind(
        IType type,
        ObjectValueNode valueSyntax,
        out GeoJsonGeometryType geometryType)
    {
        var fields = valueSyntax.Fields;

        for (var i = 0; i < fields.Count; i++)
        {
            if (fields[i].Name.Value == TypeFieldName
                && GeoJsonTypeSerializer.Default.CoerceInputLiteral(type, fields[i].Value) is
                    GeoJsonGeometryType gt)
            {
                geometryType = gt;
                return true;
            }
        }

        geometryType = default;
        return false;
    }

    private bool TryGetGeometryKindFromJson(
        IType type,
        JsonElement inputValue,
        out GeoJsonGeometryType geometryType)
    {
        if (inputValue.TryGetProperty(TypeFieldName, out var typeElement)
            && typeElement.ValueKind == JsonValueKind.String)
        {
            var typeName = typeElement.GetString();
            if (typeName is not null
                && GeoJsonTypeSerializer.Default.TryParseString(typeName, out geometryType))
            {
                return true;
            }
        }

        geometryType = default;
        return false;
    }

    public static readonly GeoJsonGeometrySerializer Default = new();
}
