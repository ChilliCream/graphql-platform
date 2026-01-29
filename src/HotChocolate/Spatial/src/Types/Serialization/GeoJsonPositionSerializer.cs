using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Serialization;

internal class GeoJsonPositionSerializer : GeoJsonSerializerBase<Coordinate>
{
    public override bool IsValueCompatible(IType type, IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (valueLiteral is NullValueNode)
        {
            return true;
        }

        if (valueLiteral is ListValueNode listValueNode)
        {
            var numberOfItems = listValueNode.Items.Count;

            if (numberOfItems != 2 && numberOfItems != 3)
            {
                return false;
            }

            if (listValueNode.Items[0] is IFloatValueLiteral
                && listValueNode.Items[1] is IFloatValueLiteral)
            {
                if (numberOfItems == 2)
                {
                    return true;
                }

                return listValueNode.Items[2] is IFloatValueLiteral;
            }
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
            var length = inputValue.GetArrayLength();
            return length is 2 or 3;
        }

        return false;
    }

    public override object? CoerceInputLiteral(IType type, IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (valueLiteral is null)
        {
            throw ThrowHelper.PositionScalar_CoordinatesCannotBeNull(null!);
        }

        if (valueLiteral is NullValueNode)
        {
            return null;
        }

        if (valueLiteral is ListValueNode list)
        {
            if (list.Items.Count != 2 && list.Items.Count != 3)
            {
                throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
            }

            if (list.Items[0] is IFloatValueLiteral x
                && list.Items[1] is IFloatValueLiteral y)
            {
                if (list.Items.Count == 2)
                {
                    return new Coordinate(x.ToDouble(), y.ToDouble());
                }

                if (list.Items.Count == 3
                    && list.Items[2] is IFloatValueLiteral z)
                {
                    return new CoordinateZ(x.ToDouble(), y.ToDouble(), z.ToDouble());
                }
            }

            throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
        }

        throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
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
            throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
        }

        var length = inputValue.GetArrayLength();
        if (length < 2 || length > 3)
        {
            throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
        }

        var x = inputValue[0].GetDouble();
        var y = inputValue[1].GetDouble();

        if (double.IsInfinity(x) || double.IsInfinity(y))
        {
            throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
        }

        if (length == 2)
        {
            return new Coordinate(x, y);
        }

        var z = inputValue[2].GetDouble();
        if (double.IsInfinity(z))
        {
            throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
        }

        return new CoordinateZ(x, y, z);
    }

    public override void CoerceOutputValue(IType type, object runtimeValue, ResultElement resultValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        CoerceOutputCoordinates(type, runtimeValue, resultValue);
    }

    public override void CoerceOutputCoordinates(IType type, object runtimeValue, ResultElement resultElement)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is not Coordinate coordinate)
        {
            throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
        }

        var hasZ = !double.IsNaN(coordinate.Z);
        resultElement.SetArrayValue(hasZ ? 3 : 2);

        var index = 0;
        foreach (var element in resultElement.EnumerateArray())
        {
            switch (index++)
            {
                case 0:
                    element.SetNumberValue(coordinate.X);
                    break;
                case 1:
                    element.SetNumberValue(coordinate.Y);
                    break;
                case 2:
                    element.SetNumberValue(coordinate.Z);
                    break;
            }
        }
    }

    public override IValueNode ValueToLiteral(IType type, object? runtimeValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        double x;
        double y;
        double z;
        switch (runtimeValue)
        {
            case Coordinate coordinate:
                x = coordinate.X;
                y = coordinate.Y;
                z = coordinate.Z;
                break;

            case double[] { Length: > 1 and < 4 } coordinateArray:
                x = coordinateArray[0];
                y = coordinateArray[1];
                z = coordinateArray.Length == 3 ? coordinateArray[2] : double.NaN;
                break;

            default:
                throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
        }

        var xNode = new FloatValueNode(x);
        var yNode = new FloatValueNode(y);

        if (!double.IsNaN(z))
        {
            var zNode = new FloatValueNode(z);
            return new ListValueNode(xNode, yNode, zNode);
        }

        return new ListValueNode(xNode, yNode);
    }

    public override IValueNode CoordinateToLiteral(IType type, object? runtimeValue)
    {
        return ValueToLiteral(type, runtimeValue);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        throw new NotSupportedException("Position scalars don't support CreateInstance");
    }

    public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
    {
        throw new NotSupportedException("Position scalars don't support GetFieldData");
    }

    public static readonly GeoJsonPositionSerializer Default = new();
}
