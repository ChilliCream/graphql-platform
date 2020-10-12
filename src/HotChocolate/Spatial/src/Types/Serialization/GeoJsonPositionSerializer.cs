using System;
using System.Collections;
using HotChocolate.Language;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Serialization
{
    internal class GeoJsonPositionSerializer : GeoJsonSerializerBase<Coordinate>
    {
        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            if (valueSyntax is NullValueNode)
            {
                return true;
            }

            if (valueSyntax is ListValueNode listValueNode)
            {
                var numberOfItems = listValueNode.Items.Count;

                if (numberOfItems != 2 && numberOfItems != 3)
                {
                    return false;
                }

                if (listValueNode.Items[0] is IFloatValueLiteral &&
                    listValueNode.Items[1] is IFloatValueLiteral)
                {
                    if (numberOfItems == 2)
                    {
                        return true;
                    }

                   if (listValueNode.Items[2] is IFloatValueLiteral)
                   {
                       return true;
                   }
                }
            }

            return false;
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax == null)
            {
                throw ThrowHelper.PositionScalar_CoordinatesCannotBeNull(null!);
            }

            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            if (valueSyntax is ListValueNode list)
            {
                if (list.Items.Count != 2 && list.Items.Count != 3)
                {
                    throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
                }

                if (list.Items[0] is IFloatValueLiteral x &&
                    list.Items[1] is IFloatValueLiteral y)
                {
                    if (list.Items.Count == 2)
                    {
                        return new Coordinate(x.ToDouble(), y.ToDouble());
                    }

                    if (list.Items.Count == 3 &&
                        list.Items[2] is IFloatValueLiteral z)
                    {
                        return new CoordinateZ(x.ToDouble(), y.ToDouble(), z.ToDouble());
                    }
                }

                throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
            }

            throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
        }

        public override IValueNode ParseValue(object? value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (!(value is Coordinate coordinate))
            {
                throw ThrowHelper.PositionScalar_CoordinatesCannotBeNull(null!);
            }

            var xNode = new FloatValueNode(coordinate.X);
            var yNode = new FloatValueNode(coordinate.Y);

            if (!double.IsNaN(coordinate.Z))
            {
                var zNode = new FloatValueNode(coordinate.Z);
                return new ListValueNode(xNode, yNode, zNode);
            }

            return new ListValueNode(xNode, yNode);
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is Coordinate coords)
            {
                if (coords.Z is double.NaN)
                {
                    return new ListValueNode(
                        new FloatValueNode(coords.X),
                        new FloatValueNode(coords.Y));
                }

                return new ListValueNode(
                    new FloatValueNode(coords.X),
                    new FloatValueNode(coords.Y),
                    new FloatValueNode(coords.Z));
            }

            if (!(resultValue is double[] coordinate))
            {
                throw ThrowHelper.PositionScalar_CoordinatesCannotBeNull(null!);
            }

            if (coordinate.Length != 2 && coordinate.Length != 3)
            {
                throw ThrowHelper.PositionScalar_InvalidPositionObject(null!);
            }

            var xNode = new FloatValueNode(coordinate[0]);
            var yNode = new FloatValueNode(coordinate[1]);

            if (coordinate.Length > 2)
            {
                var zNode = new FloatValueNode(coordinate[2]);
                return new ListValueNode(xNode, yNode, zNode);
            }

            return new ListValueNode(xNode, yNode);
        }

        public override bool TryDeserialize(object? serialized, out object? value)
        {
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

            if (list.Count < 2 || list.Count > 3)
            {
                value = null;
                return false;
            }

            double x;
            double y;
            try
            {
                x = Convert.ToDouble(list[0]);
                y = Convert.ToDouble(list[1]);
            }
            catch (Exception ex) when (ex is FormatException ||
                ex is InvalidCastException ||
                ex is OverflowException)
            {
                value = null;
                return false;
            }

            if (double.IsInfinity(x) || double.IsInfinity(y))
            {
                value = null;
                return false;
            }

            if (list.Count == 2)
            {
                value = new Coordinate(x, y);
                return true;
            }

            try
            {
                var z = Convert.ToDouble(list[2]);
                if (double.IsInfinity(z))
                {
                    value = null;
                    return false;
                }

                value = new CoordinateZ(x, y, z);
                return true;
            }
            catch (Exception ex) when (ex is FormatException ||
                ex is InvalidCastException ||
                ex is OverflowException)
            {
                value = null;
                return false;
            }
        }

        public override bool TrySerialize(object? value, out object? serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (!(value is Coordinate coordinate))
            {
                serialized = null;
                return false;
            }

            if (!double.IsNaN(coordinate.Z))
            {
                serialized = new[] { coordinate.X, coordinate.Y, coordinate.Z };
                return true;
            }

            serialized = new[] { coordinate.X, coordinate.Y };
            return true;
        }

        public static readonly GeoJsonPositionSerializer Default = new GeoJsonPositionSerializer();
    }
}
