using System;
using HotChocolate.Language;
using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Scalar
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

            return literal is ObjectValueNode
                   || literal is StringValueNode
                   || literal is NullValueNode;
        }

        /// i don't know what this is?
        public override object ParseLiteral(IValueNode literal)
        {
            return literal.Value;
        }

        /// input value from the client
        public override IValueNode ParseValue(object value)
        {
            return null;
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value is Coordinate coordinate)
            {
                if (!double.IsNaN(coordinate.Z))
                {
                    serialized = new double[] { coordinate.X, coordinate.Y, coordinate.Z };

                    return true;
                }

                serialized = new double[] { coordinate.X, coordinate.Y };

                return true;
            }

            throw new ArgumentException(
                "The specified value cannot be serialized as a Coordinate.");
        }
    }
}
