using System;
using System.Collections;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using HotChocolate.Types.Spatial.Properties;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONPositionScalar : ScalarType<Coordinate>
    {
        /// https://tools.ietf.org/html/rfc7946#section-3.1.1
        public GeoJSONPositionScalar() : base("Position")
        {
            Description = Resources.GeoJSONPositionScalar_Description;
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is null)
            {
                throw ThrowHelper.NullPositionScalar();
            }

            if (literal is NullValueNode)
            {
                return true;
            }

            if (literal is ListValueNode listValueNode)
            {
                if (listValueNode.Items.Count != 2 && listValueNode.Items.Count != 3)
                {
                    return false;
                }

                if (listValueNode.Items[0] is IFloatValueLiteral &&
                    listValueNode.Items[1] is IFloatValueLiteral)
                {
                    if (listValueNode.Items.Count == 2)
                    {
                        return true;
                    }
                    else if (listValueNode.Items.Count == 3 &&
                        listValueNode.Items[2] is IFloatValueLiteral)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override object? ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw ThrowHelper.NullPositionScalar();
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            if (literal is ListValueNode list)
            {
                if (list.Items.Count != 2 && list.Items.Count != 3)
                {
                    throw ThrowHelper.InvalidPositionScalar();
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

                throw ThrowHelper.InvalidPositionScalar();
            }

            throw ThrowHelper.InvalidPositionScalar();
        }

        /// input value from the client
        public override IValueNode ParseValue(object? value)
        {
            // parse Coordinate into literal
            if (value is null)
            {
                return new NullValueNode(null);
            }

            if (!(value is Coordinate coordinate))
            {
                // TODO : ThrowHelper
                throw new ArgumentException("The specified value has to be a Coordinate Type");
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

            if (list.Count < 2)
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
            catch (OverflowException)
            {
                value = null;
                return false;
            }

            var coordinate = new Coordinate(x, y);
            if (list.Count > 2)
            {
                try
                {
                    coordinate = new CoordinateZ(x, y, Convert.ToDouble(list[2]));
                }
                catch (OverflowException)
                {
                    value = null;
                    return false;
                }
            }

            value = coordinate;
            return true;
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
                serialized = new double[] { coordinate.X, coordinate.Y, coordinate.Z };
                return true;
            }

            serialized = new double[] { coordinate.X, coordinate.Y };
            return true;
        }
    }
}
