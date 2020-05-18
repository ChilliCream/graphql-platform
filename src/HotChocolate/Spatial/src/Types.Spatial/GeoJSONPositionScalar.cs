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
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return true;
            }

            if (literal is ListValueNode listValueNode)
            {
                for (var i = 0; i < listValueNode.Items.Count; i++)
                {
                    if (listValueNode.Items[i].Value is ListValueNode || listValueNode.Items[i].Value is IList)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override object? ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                // TODO : move throwhelper
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            if (literal is ListValueNode list)
            {
                if (list.Items.Count != 2 && list.Items.Count != 3)
                {
                    // TODO : move throwhelper
                    throw new ScalarSerializationException(
                        "The position type has to have two or three elements [x,y] or [x,y,z]");
                }

                if (list.Items[0] is IFloatValueLiteral x &&
                    list.Items[1] is IFloatValueLiteral y)
                {
                    if (list.Items.Count == 2)
                    {
                        return new Coordinate(x.ToDouble(), y.ToDouble());
                    }
                    else if (list.Items.Count == 3 &&
                        list.Items[1] is IFloatValueLiteral z)
                    {
                        return new CoordinateZ(x.ToDouble(), y.ToDouble(), z.ToDouble());
                    }
                }

                // TODO : move throwhelper
                throw new ScalarSerializationException(
                    "All elements of the scalar have to be int or float literals.");
            }

            // TODO : move throwhelper
            throw new ScalarSerializationException(
                "A valid position has to be a list or two [x,y] or three [x,y,z] elements " +
                "representing a position.");
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
                serialized = new double[] { coordinate.X, coordinate.Y, coordinate.Z };

                return true;
            }

            serialized = new double[] { coordinate.X, coordinate.Y };

            return true;
        }
    }
}
