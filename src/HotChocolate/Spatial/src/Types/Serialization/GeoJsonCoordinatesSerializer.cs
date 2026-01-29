using System.Collections;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Serialization;

internal class GeoJsonCoordinatesSerializer : GeoJsonSerializerBase
{
    public override bool IsValueCompatible(IType type, IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (valueLiteral is NullValueNode)
        {
            return true;
        }

        if (valueLiteral is ListValueNode)
        {
            return true;
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

        if (inputValue.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        return false;
    }

    public override object? CoerceInputLiteral(IType type, IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (valueLiteral is null)
        {
            throw ThrowHelper.CoordinatesScalar_CoordinatesCannotBeNull(null!);
        }

        if (valueLiteral is NullValueNode)
        {
            return null;
        }

        if (valueLiteral is ListValueNode list)
        {
            return ParseCoordinateLiteral(type, list);
        }

        throw ThrowHelper.CoordinatesScalar_InvalidCoordinatesObject(null!);
    }

    public override object? CoerceInputValue(IType type, JsonElement inputValue, IFeatureProvider context)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (inputValue.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (inputValue.ValueKind != JsonValueKind.Array)
        {
            throw ThrowHelper.CoordinatesScalar_InvalidCoordinatesObject(null!);
        }

        return ParseCoordinateFromJson(type, inputValue);
    }

    public override void CoerceOutputValue(IType type, object runtimeValue, ResultElement resultValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        CoerceOutputCoordinates(type, runtimeValue, resultValue);
    }

    public override void CoerceOutputCoordinates(IType type, object runtimeValue, ResultElement resultElement)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is Coordinate coord)
        {
            GeoJsonPositionSerializer.Default.CoerceOutputCoordinates(type, coord, resultElement);
            return;
        }

        if (runtimeValue is IList list)
        {
            resultElement.SetArrayValue(list.Count);

            var index = 0;
            foreach (var element in resultElement.EnumerateArray())
            {
                CoerceOutputCoordinates(type, list[index++]!, element);
            }

            return;
        }

        throw ThrowHelper.CoordinatesScalar_InvalidCoordinatesObject(type);
    }

    public override IValueNode ValueToLiteral(IType type, object? runtimeValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is Coordinate)
        {
            return GeoJsonPositionSerializer.Default.ValueToLiteral(type, runtimeValue);
        }

        if (runtimeValue is double[])
        {
            return GeoJsonPositionSerializer.Default.ValueToLiteral(type, runtimeValue);
        }

        if (runtimeValue is IList list)
        {
            var results = new IValueNode[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                results[i] = ValueToLiteral(type, list[i]);
            }

            return new ListValueNode(results);
        }

        throw ThrowHelper.CoordinatesScalar_InvalidCoordinatesObject(type);
    }

    public override IValueNode CoordinateToLiteral(IType type, object? runtimeValue)
    {
        if (runtimeValue is IList list)
        {
            var results = new IValueNode[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                results[i] = CoordinateToLiteral(type, list[i]);
            }

            return new ListValueNode(results);
        }

        if (runtimeValue is Coordinate)
        {
            return GeoJsonPositionSerializer.Default.ValueToLiteral(type, runtimeValue);
        }

        if (runtimeValue is double[])
        {
            return GeoJsonPositionSerializer.Default.ValueToLiteral(type, runtimeValue);
        }

        if (runtimeValue is Geometry g
            && GeoJsonSerializers.SerializersByTypeName
                .TryGetValue(g.GeometryType, out var serializer))
        {
            return serializer.CoordinateToLiteral(type, runtimeValue);
        }

        throw ThrowHelper.Serializer_CouldNotParseValue(type);
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

    public new object ParseCoordinateLiteral(IType type, IValueNode syntaxNode)
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

            if (GeoJsonPositionSerializer.Default.CoerceInputLiteral(type, top) is Coordinate coord)
            {
                return coord;
            }
        }

        throw ThrowHelper.Serializer_Parse_CoordinatesIsInvalid(type);
    }

    public object ParseCoordinateFromJson(IType type, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() == 0)
        {
            throw ThrowHelper.Serializer_Parse_CoordinatesIsInvalid(type);
        }

        // Check if this is a coordinate (array of numbers) or nested array
        if (element[0].ValueKind == JsonValueKind.Number)
        {
            // This is a position [x, y] or [x, y, z]
            return GeoJsonPositionSerializer.Default.CoerceInputValue(type, element, null!)!;
        }

        // This is a nested array - recurse
        var result = new object[element.GetArrayLength()];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = ParseCoordinateFromJson(type, element[i]);
        }

        return result;
    }

    public static readonly GeoJsonCoordinatesSerializer Default = new();
}
