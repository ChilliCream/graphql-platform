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
        private const string _longitude = "longitude";
        private const string _latitude = "latitude";
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
            descriptor.Field("longitude")
                .Type<NonNullType<FloatType>>();

            descriptor.Field("latitude")
                .Type<NonNullType<FloatType>>();

            // optional fields
            descriptor.Field("srid")
                .Type<IntType>();
        }

        public override object? ParseLiteral(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return null;
            }

            if (literal is ObjectValueNode obj && obj.Fields.Count >= 2)
            {
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

                if (longitude.HasValue && latitude.HasValue)
                {
                    return srid.HasValue
                        ? throw new System.Exception("Factory goes here.")
                        : new Point(longitude.Value, latitude.Value);
                }
            }

            // TODO : resources
            throw new InputObjectSerializationException("ERROR_MESSAGE");
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
