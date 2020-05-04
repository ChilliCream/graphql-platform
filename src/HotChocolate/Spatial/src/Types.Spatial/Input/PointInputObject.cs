using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities.Serialization;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Input
{
    public class PointInputObject: InputObjectType<Point>
    {
        protected override void Configure(IInputObjectTypeDescriptor<Point> descriptor)
        {
            base.Configure(descriptor);

            descriptor.BindFieldsExplicitly();

            // required fields
            descriptor.Field("longitude")
                .Type<NonNullType<FloatType>>();

            descriptor.Field("latitude")
                .Type<NonNullType<FloatType>>();

            // optional fields
            descriptor.Field("srid")
                .Type<IntType>();
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal is ObjectValueNode obj)
            {
                var srid = obj.Fields.FirstOrDefault(o => o.Name.Value == "longitude").Value.Value;

                // var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: srid);

            }
            return new Point(1337, 1337);

            return base.ParseLiteral(literal);
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            return base.TrySerialize(value, out serialized);
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            return base.TryDeserialize(serialized, out value);
        }
    }
}
