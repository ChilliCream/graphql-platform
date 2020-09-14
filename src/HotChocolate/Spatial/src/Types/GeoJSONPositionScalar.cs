using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using HotChocolate.Types.Spatial.Properties;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONPositionScalar : ScalarType<Coordinate>
    {
        /// https://tools.ietf.org/html/rfc7946#section-3.1.1
        public GeoJSONPositionScalar() : base("Position")
        {
            Description = Resources.GeoJSONPositionScalar_Description;
        }

        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            if (valueSyntax is NullValueNode)
            {
                return true;
            }

            if (valueSyntax is ListValueNode listValueNode)
            {
                int numberOfItems = listValueNode.Items.Count;

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
                    else if (numberOfItems == 3 &&
                        listValueNode.Items[2] is IFloatValueLiteral)
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
                throw PositionScalar_CoordinatesCannotBeNull(this);
            }

            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            if (valueSyntax is ListValueNode list)
            {
                if (list.Items.Count != 2 && list.Items.Count != 3)
                {
                    throw PositionScalar_InvalidPositionObject(this);
                }

                if (list.Items[0] is IFloatValueLiteral x &&
                    list.Items[1] is IFloatValueLiteral y)
                {
                    if (list.Items.Count == 2)
                    {
                        return new Coordinate(x.ToDouble(), y.ToDouble());
                    }
                    else if (list.Items.Count == 3 &&
                        list.Items[2] is IFloatValueLiteral z)
                    {
                        return new CoordinateZ(x.ToDouble(), y.ToDouble(), z.ToDouble());
                    }
                }

                throw PositionScalar_InvalidPositionObject(this);
            }

            throw PositionScalar_InvalidPositionObject(this);
        }

        /// input value from the client
        public override IValueNode ParseValue(object? value)
        {
            // parse Coordinate into valueSyntax
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (!(value is Coordinate coordinate))
            {
                throw PositionScalar_CoordinatesCannotBeNull(this);
            }

            var xNode = new FloatValueNode(coordinate.X);
            var yNode = new FloatValueNode(coordinate.Y);

            // third optional element (z/elevation)
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

            if (!(resultValue is IList<double> coordinate))
            {
                throw PositionScalar_CoordinatesCannotBeNull(this);
            }

            var xNode = new FloatValueNode(coordinate[0]);
            var yNode = new FloatValueNode(coordinate[1]);

            // third optional element (z/elevation)
            if (coordinate.Count > 2)
            {
                var zNode = new FloatValueNode(coordinate[2]);
                return new ListValueNode(xNode, yNode, zNode);
            }

            return new ListValueNode(xNode, yNode);
        }

        public override bool TryDeserialize(object? serialized, out object? value)
        {
            // Deserialize from graphql variable
            // List<object> (long) = List<long>

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
                double z = Convert.ToDouble(list[2]);
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
            if (!(value is Coordinate coordinate))
            {
                serialized = null;
                return false;
            }

            if (!double.IsNaN(coordinate.Z))
            {
                serialized = new double[] {coordinate.X, coordinate.Y, coordinate.Z};
                return true;
            }

            serialized = new double[] {coordinate.X, coordinate.Y};
            return true;
        }
    }
}
