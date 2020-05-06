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
        private const string _xFieldName = "x";
        private const string _yFieldName = "y";
        private const string _sridFieldName = "srid";

        private IInputField _xField = default!;
        private IInputField _yField = default!;
        private IInputField _sridField = default!;

        public PointInputObject() {}

        protected override void Configure(IInputObjectTypeDescriptor<Point> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            // required fields
            descriptor.Field(_xFieldName)
                .Type<NonNullType<FloatType>>()
                .Description("X or Longitude");

            descriptor.Field(_yFieldName)
                .Type<NonNullType<FloatType>>()
                .Description("Y or Latitude");

            // optional fields
            descriptor.Field(_sridFieldName)
                .Type<IntType>()
                .Description("Spatial Reference System Identifier. e.g. latitude/longitude (WGS84): 4326, web mercator: 3867");
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

            double? x = null;
            double? y = null;
            int? srid = null;

            for (int i = 0; i < obj.Fields.Count; i++)
            {
                ObjectFieldNode field = obj.Fields[i];

                switch (field.Name.Value)
                {
                    case _xFieldName:
                        x = (double)_xField.Type.ParseLiteral(field.Value);
                        break;
                    case _yFieldName:
                        y = (double)_yField.Type.ParseLiteral(field.Value);
                        break;
                    case _sridFieldName:
                        srid = (int)_sridField.Type.ParseLiteral(field.Value);
                        break;
                }
            }

            if (!x.HasValue || !y.HasValue)
            {
                throw new InputObjectSerializationException(
                    "Failed to serialize PointInputObject. You have to at least specify x and y");
            }

            if (!srid.HasValue)
            {
                return new Point(x.Value, y.Value);
            }

            var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid.Value);

            return factory.CreatePoint(new Coordinate(x.Value, y.Value));
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
            _xField = Fields[_xFieldName];
            _yField = Fields[_yFieldName];
            _sridField = Fields[_sridFieldName];
        }
    }
}
