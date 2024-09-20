using HotChocolate.Language;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.Serialization.GeoJsonSerializers;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Serialization;

internal sealed class GeoJsonGeometrySerializer : IGeoJsonSerializer
{
    public bool TrySerialize(IType type, object? runtimeValue, out object? resultValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        try
        {
            resultValue = Serialize(type, runtimeValue);
            return true;
        }
        catch
        {
            resultValue = null;
            return false;
        }
    }

    public bool IsInstanceOfType(IType type, IValueNode valueSyntax)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (valueSyntax is null)
        {
            throw new ArgumentNullException(nameof(valueSyntax));
        }

        if (valueSyntax is NullValueNode)
        {
            return true;
        }

        if (valueSyntax.Kind != SyntaxKind.ObjectValue)
        {
            return false;
        }

        return GetGeometrySerializer(type, (ObjectValueNode)valueSyntax)
            .IsInstanceOfType(type, valueSyntax);
    }

    public bool IsInstanceOfType(IType type, object? runtimeValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return runtimeValue is Geometry &&
            SerializersByType.TryGetValue(runtimeValue.GetType(), out var serializer) &&
            serializer.IsInstanceOfType(type, runtimeValue);
    }

    public object? ParseLiteral(IType type, IValueNode valueSyntax)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (valueSyntax.Kind is SyntaxKind.NullValue)
        {
            return null;
        }

        if (valueSyntax.Kind is not SyntaxKind.ObjectValue)
        {
            throw Serializer_Parse_ValueKindInvalid(type, valueSyntax.Kind);
        }

        return GetGeometrySerializer(type, (ObjectValueNode)valueSyntax)
            .ParseLiteral(type, valueSyntax);
    }

    public IValueNode ParseResult(IType type, object? resultValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is Geometry)
        {
            return ParseValue(type, resultValue);
        }

        if (resultValue is IReadOnlyDictionary<string, object> dict)
        {
            if (!dict.TryGetValue(TypeFieldName, out var typeObject) ||
                typeObject is not string typeName)
            {
                throw Serializer_TypeIsMissing(type);
            }

            if (GeoJsonTypeSerializer.Default.TryParseString(
                    typeName,
                    out var kind))
            {
                return Serializers[kind].ParseResult(type, resultValue);
            }

            throw Geometry_Parse_InvalidGeometryKind(type, typeName);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public IValueNode ParseValue(IType type, object? runtimeValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is IReadOnlyDictionary<string, object> ||
            runtimeValue is IDictionary<string, object>)
        {
            return ParseResult(type, runtimeValue);
        }

        if (!(runtimeValue is Geometry geometry))
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

        return geometryType.ParseValue(type, runtimeValue);
    }

    public object? Serialize(IType type, object? runtimeValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (runtimeValue is null)
        {
            return null;
        }

        if (runtimeValue is IReadOnlyDictionary<string, object> ||
            runtimeValue is IDictionary<string, object>)
        {
            return runtimeValue;
        }

        if (runtimeValue is not Geometry geometry)
        {
            throw Geometry_Serialize_InvalidGeometryType(type, runtimeValue.GetType());
        }

        if (!TryGetGeometryKind(geometry, out var kind))
        {
            throw Geometry_Serialize_TypeIsUnknown(type, geometry.GeometryType);
        }

        return Serializers[kind].Serialize(type, runtimeValue);
    }

    public object? Deserialize(IType type, object? resultValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (resultValue is null)
        {
            return null;
        }

        if (resultValue is Geometry geometry)
        {
            if (!TryGetGeometryKind(geometry, out var kind))
            {
                throw Geometry_Deserialize_TypeIsUnknown(type, geometry.GeometryType);
            }

            return Serializers[kind].Deserialize(type, resultValue);
        }

        if (resultValue is IReadOnlyDictionary<string, object> dict)
        {
            if (!dict.TryGetValue(TypeFieldName, out var typeObject) ||
                typeObject is not string typeName)
            {
                throw Geometry_Deserialize_TypeIsMissing(type);
            }

            if (GeoJsonTypeSerializer.Default.TryParseString(
                    typeName,
                    out var kind))
            {
                return Serializers[kind].Deserialize(type, resultValue);
            }

            throw Geometry_Deserialize_TypeIsUnknown(type, typeName);
        }

        throw Serializer_CouldNotDeserialize(type);
    }

    public bool TryDeserialize(IType type, object? resultValue, out object? runtimeValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        try
        {
            runtimeValue = Deserialize(type, resultValue);
            return false;
        }
        catch
        {
            runtimeValue = null;
            return false;
        }
    }

    public object CreateInstance(IType type, object?[] fieldValues)
        => throw Serializer_OperationIsNotSupported(type,
            this,
            nameof(CreateInstance));

    public void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
        => throw Serializer_OperationIsNotSupported(type,
            this,
            nameof(GetFieldData));

    public bool TrySerializeCoordinates(
        IType type,
        object runtimeValue,
        out object? serialized)
        => throw Serializer_OperationIsNotSupported(type,
            this,
            nameof(TrySerializeCoordinates));

    public IValueNode ParseCoordinateValue(IType type, object? runtimeValue)
        => throw Serializer_OperationIsNotSupported(type,
            this,
            nameof(ParseCoordinateValue));

    public IValueNode ParseCoordinateResult(IType type, object? runtimeValue)
        => throw Serializer_OperationIsNotSupported(type,
            this,
            nameof(ParseCoordinateResult));

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
            if (fields[i].Name.Value == TypeFieldName &&
                GeoJsonTypeSerializer.Default.ParseLiteral(type, fields[i].Value) is
                    GeoJsonGeometryType gt)
            {
                geometryType = gt;
                return true;
            }
        }

        geometryType = default;
        return false;
    }

    public static readonly GeoJsonGeometrySerializer Default = new();
}
