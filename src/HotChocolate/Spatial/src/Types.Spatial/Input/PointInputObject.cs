using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities.Serialization;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Input
{
    public class PointInputObject : InputObjectType<Point>
    {
        private const string _longitude = "x";
        private const string _latitude = "y";
        private const string _srid = "srid";

        private IInputField _longitudeField;
        private IInputField _latitudeField;
        private IInputField _sridField;

        public PointInputObject()
        {
            _longitudeField = default!;
            _latitudeField = default!;
            _sridField = default!;
        }

        protected override void Configure(IInputObjectTypeDescriptor<Point> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            // required fields
            descriptor.Field(_longitude)
                .Type<NonNullType<FloatType>>()
                .Description("X or Longitude");

            descriptor.Field(_latitude)
                .Type<NonNullType<FloatType>>()
                .Description("Y or Latitude");

            // optional fields
            descriptor.Field(_srid)
                .Type<IntType>()
                .Description("Spatial Reference System Identifier");
        }

        public override object? ParseLiteral(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return null;
            }

            if (!(literal is ObjectValueNode obj) || obj.Fields.Count < 2)
            {
                throw new InputObjectSerializationException(
                    "Failed to serialize PointInputObject. Needs at least two input fields");
            }

            double? longitude = null;
            double? latitude = null;
            int? srid = null;

            for (int i = 0; i < obj.Fields.Count; i++)
            {
                ObjectFieldNode field = obj.Fields[i];

                switch (field.Name.Value)
                {
                    case _longitude:
                        longitude = (double)_longitudeField.Type.ParseLiteral(field.Value);
                        break;
                    case _latitude:
                        latitude = (double)_latitudeField.Type.ParseLiteral(field.Value);
                        break;
                    case _srid:
                        srid = (int)_sridField.Type.ParseLiteral(field.Value);
                        break;
                }
            }

            if (!longitude.HasValue || !latitude.HasValue)
            {
                throw new InputObjectSerializationException(
                    "Failed to serialize PointInputObject. You have to at least specify x and y");
            }

            if (!srid.HasValue)
            {
                return new Point(longitude.Value, latitude.Value);
            }

            var factory = new NtsGeometryServices().CreateGeometryFactory(srid.Value);

            return factory.CreatePoint(new Coordinate(longitude.Value, latitude.Value));
        }

        public override bool TrySerialize(object value, out object? serialized)
        {
            return base.TrySerialize(value, out serialized);
        }

        public override bool TryDeserialize(object serialized, out object? value)
        {
            return base.TryDeserialize(serialized, out value);
        }

        protected override void OnAfterCompleteType(
            ICompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            _longitudeField = Fields[_longitude];
            _latitudeField = Fields[_latitude];
            _sridField = Fields[_srid];
        }
    }
}
