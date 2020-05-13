using System;
using System.Collections;
using HotChocolate.Language;
using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace Types.Spatial
{
    public class GeoJSONPositionScalar : ScalarType<Coordinate>
    {
        /// https://tools.ietf.org/html/rfc7946#section-3.1.1
        public GeoJSONPositionScalar() : base("Position")
        {
            Description = "A position is an array of numbers. There MUST be two or more elements.";
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
                throw new ArgumentNullException(nameof(literal));

            return literal is ListValueNode || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            // parse literal from input (ListValueNode) into Coordinate

            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            if (literal is ListValueNode listNode)
            {
                if (listNode.Items.Count != 2 && listNode.Items.Count != 3)
                {
                    throw new ArgumentException(
                        "The Position type has to have two or three elements [x,y] or [x,y,z]",
                        nameof(literal));
                }

                var xNode = listNode.Items[0];
                var yNode = listNode.Items[1];
                if (xNode == null)
                {
                    throw new ArgumentException(
                        "The first element of the array can not be null",
                        nameof(literal));
                }

                if (yNode == null)
                {
                    throw new ArgumentException(
                        "The second element of the array can not be null",
                        nameof(literal));
                }

                var xValue = xNode switch
                {
                    FloatValueNode node => node.ToDouble(),
                    IntValueNode node => node.ToDouble(),
                    _ => throw new ArgumentException(
                        "Couldn't convert element of the array to double", nameof(literal)),
                };
                var yValue = yNode switch
                {
                    FloatValueNode node => node.ToDouble(),
                    IntValueNode node => node.ToDouble(),
                    _ => throw new ArgumentException(
                        "Couldn't convert element of the array to double", nameof(literal)),
                };

                // optional third element (z/elevation)
                var coordinate = new Coordinate(xValue, yValue);
                if (listNode.Items.Count == 3)
                {
                    var zNode = listNode.Items[2];
                    var zValue = zNode switch
                    {
                        FloatValueNode node => node.ToDouble(),
                        IntValueNode node => node.ToDouble(),
                        _ => throw new ArgumentException(
                            "Couldn't convert member of the array to double", nameof(literal)),
                    };

                    coordinate = new CoordinateZ(xValue, yValue, zValue);
                }

                return coordinate;
            }

            throw new ArgumentException(
                "The Position type can only parse List literals.",
                nameof(literal));
        }

        /// input value from the client
        public override IValueNode ParseValue(object value)
        {
            // parse Coordinate into literal
            if (value == null) return new NullValueNode(null);

            if (value is Coordinate coordinate)
            {
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

            throw new ArgumentException("The specified value has to be a Coordinate Type");
        }

        public override bool TryDeserialize(object serialized, out object value)
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
                throw new ArgumentException(
                    "The Position type has to at least contain two values (x,y)",
                    nameof(serialized));
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
                throw new ArgumentException(
                    "Members of the Position array have to be of type double");
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
                    throw new ArgumentException(
                        "Members of the Position array have to be of type double");
                }
            }

            value = coordinate;
            return true;
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            if (!(value is Coordinate coordinate))
            {
                throw new ArgumentException(
                    "The specified value cannot be serialized as a Coordinate.");
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
