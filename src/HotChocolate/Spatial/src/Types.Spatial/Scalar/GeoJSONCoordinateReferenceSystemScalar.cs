using System;
using HotChocolate.Language;
using HotChocolate.Types;
using Types.Spatial.Output;

namespace Types.Spatial.Scalar
{
    public class GeoJSONCoordinateReferenceSystemScalar : ScalarType<GeoJSONCoordinateReferenceSystem>
    {
        /// https://tools.ietf.org/html/rfc7946#section-3.1.1
        public GeoJSONCoordinateReferenceSystemScalar() : base("CRS")
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
            serialized = new GeoJSONCoordinateReferenceSystem {
                Type = CRSType.Name,
                Properties = new CRSProperties {
                    Name = "urn:ogc:def:crs:OGC::CRS84"
                }
            };

            return true;
        }
    }
}
