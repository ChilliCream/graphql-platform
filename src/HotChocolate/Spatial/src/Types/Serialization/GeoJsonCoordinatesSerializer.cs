using System;
using System.Collections;
using HotChocolate.Language;
using HotChocolate.Types.Spatial.Serialization;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

internal class GeoJsonCoordinatesSerializer : GeoJsonSerializerBase
{
    public override bool IsInstanceOfType(IType type, IValueNode valueSyntax)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (valueSyntax is NullValueNode)
        {
            return true;
        }

        if (valueSyntax is ListValueNode)
        {
            return true;
        }

        return false;
    }

    public override object? ParseLiteral(IType type, IValueNode valueSyntax)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (valueSyntax is null)
        {
            throw ThrowHelper.CoordinatesScalar_CoordinatesCannotBeNull(null!);
        }

        if (valueSyntax is NullValueNode)
        {
            return null;
        }

        if (valueSyntax is ListValueNode list)
        {
            return ParseCoordinateLiteral(type, list);
        }

        throw ThrowHelper.CoordinatesScalar_InvalidCoordinatesObject(null!);
    }

    public override IValueNode ParseValue(IType type, object? value)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (value is null)
        {
            return NullValueNode.Default;
        }

        if (value is Coordinate)
        {
            return GeoJsonPositionSerializer.Default.ParseValue(type, value);
        }

        if (value is double[])
        {
            return GeoJsonPositionSerializer.Default.ParseResult(type, value);
        }

        if (value is IList list)
        {
            var results = new IValueNode[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                results[i] = ParseValue(type, list[i]);
            }

            return new ListValueNode(results);
        }

        throw ThrowHelper.CoordinatesScalar_InvalidCoordinatesObject(type);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        throw ThrowHelper.Serializer_OperationIsNotSupported(type,
            this,
            nameof(CreateInstance));
    }

    public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
    {
        throw ThrowHelper.Serializer_OperationIsNotSupported(type,
            this,
            nameof(GetFieldData));
    }

    public override bool TrySerializeCoordinates(
        IType type,
        object runtimeValue,
        out object? serialized)
    {
        throw ThrowHelper.Serializer_OperationIsNotSupported(type,
            this,
            nameof(TrySerializeCoordinates));
    }

    public override IValueNode ParseCoordinateValue(IType type, object? runtimeValue)
    {
        if (runtimeValue is IList list)
        {
            var results = new IValueNode[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                results[i] = ParseCoordinateValue(type, list[i]);
            }

            return new ListValueNode(results);
        }

        if (runtimeValue is Coordinate)
        {
            return GeoJsonPositionSerializer.Default.ParseResult(type, runtimeValue);
        }

        if (runtimeValue is double[])
        {
            return GeoJsonPositionSerializer.Default.ParseResult(type, runtimeValue);
        }

        if (runtimeValue is Geometry g &&
            GeoJsonSerializers.SerializersByTypeName
                .TryGetValue(g.GeometryType, out var serializer))
        {
            return serializer.ParseCoordinateValue(type, runtimeValue);
        }

        throw ThrowHelper.Serializer_CouldNotParseValue(type);
    }

    public override IValueNode ParseResult(IType type, object? resultValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is Coordinate)
        {
            return GeoJsonPositionSerializer.Default.ParseResult(type, resultValue);
        }

        if (resultValue is double[])
        {
            return GeoJsonPositionSerializer.Default.ParseResult(type, resultValue);
        }

        if (resultValue is IList list)
        {
            var results = new IValueNode[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                results[i] = ParseValue(type, list[i]);
            }

            return new ListValueNode(results);
        }

        throw ThrowHelper.CoordinatesScalar_InvalidCoordinatesObject(type);
    }

    public override bool TryDeserialize(IType type, object? serialized, out object? value)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (serialized is null)
        {
            value = null;
            return true;
        }

        if (!(serialized is IList list))
        {
            value = null;
            return false;
        }

        if (list.Count >= 2 && list.Count <= 3 && list[0] is not IList)
        {
            return GeoJsonPositionSerializer.Default.TryDeserialize(type,
                serialized,
                out value);
        }

        var results = new object?[list.Count];
        for (var i = 0; i < list.Count; i++)
        {
            if (TryDeserialize(type, list[i], out var innerValue))
            {
                results[i] = innerValue;
            }
            else
            {
                value = null;
                return false;
            }
        }

        value = results;
        return true;
    }

    public override bool TrySerialize(IType type, object? value, out object? serialized)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (value is null)
        {
            serialized = null;
            return true;
        }

        if (value is Coordinate)
        {
            return GeoJsonPositionSerializer.Default.TrySerialize(type,
                value,
                out serialized);
        }

        if (value is not IList list)
        {
            serialized = null;
            return false;
        }

        var results = new object?[list.Count];
        for (var i = 0; i < list.Count; i++)
        {
            if (TrySerialize(type, list[i], out var innerSerializer))
            {
                results[i] = innerSerializer;
            }
            else
            {
                serialized = null;
                return false;
            }
        }

        serialized = results;
        return true;
    }

    public object ParseCoordinateLiteral(IType type, IValueNode syntaxNode)
    {
        if (syntaxNode is ListValueNode top && top.Items.Count > 0)
        {
            if (top.Items[0] is ListValueNode second && second.Items.Count > 0)
            {
                var result = new object[top.Items.Count];
                for (var y = 0; y < result.Length; y++)
                {
                    if (ParseCoordinateLiteral(type, top.Items[y]) is { } multi)
                    {
                        result[y] = multi;
                    }
                    else
                    {
                        throw ThrowHelper.Serializer_Parse_CoordinatesIsInvalid(type);
                    }
                }

                return result;
            }

            if (GeoJsonPositionSerializer.Default.ParseLiteral(type, top) is Coordinate coord)
            {
                return coord;
            }
        }

        throw ThrowHelper.Serializer_Parse_CoordinatesIsInvalid(type);
    }

    public object DeserializeCoordinate(IType type, object? runtimeValue)
    {
        if (runtimeValue is IList { Count: > 0, } top)
        {
            if (top[0] is IList { Count: > 0, })
            {
                var result = new object[top.Count];
                for (var y = 0; y < result.Length; y++)
                {
                    if (DeserializeCoordinate(type, top[y]) is { } multi)
                    {
                        result[y] = multi;
                    }
                    else
                    {
                        throw ThrowHelper.Serializer_Parse_CoordinatesIsInvalid(type);
                    }
                }

                return result;
            }
            else if (GeoJsonPositionSerializer.Default.TryDeserialize(
                    type,
                    runtimeValue,
                    out var result) &&
                result is not null)
            {
                return result;
            }
        }

        throw ThrowHelper.Serializer_Parse_CoordinatesIsInvalid(type);
    }

    public static readonly GeoJsonCoordinatesSerializer Default = new();
}
