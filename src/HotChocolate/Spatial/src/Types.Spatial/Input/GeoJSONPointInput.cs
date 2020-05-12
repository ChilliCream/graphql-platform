using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Types.Spatial.Common;
using Types.Spatial.Scalar;

namespace Types.Spatial.Input
{
    public class GeoJSONPointInput : InputObjectType<Point>
    {
        private const string _typeFieldName = "type";
        private const string _coordinatesFieldName = "coordinates";

        private IInputField _typeField = default!;
        private IInputField _coordinatesField = default!;

        public GeoJSONPointInput() { }

        protected override void Configure(IInputObjectTypeDescriptor<Point> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(_typeFieldName).Type<EnumType<GeoJSONGeometryType>>();

            descriptor.Field(_coordinatesFieldName).Type<GeoJSONPositionScalar>();
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

            Coordinate? coordinates = null;
            GeoJSONGeometryType? type = null;

            for (var i = 0; i < obj.Fields.Count; i++)
            {
                ObjectFieldNode field = obj.Fields[i];

                switch (field.Name.Value)
                {
                    case _coordinatesFieldName:
                        coordinates = (Coordinate)_coordinatesField.Type.ParseLiteral(field.Value);
                        break;
                    case _typeFieldName:
                        type = (GeoJSONGeometryType)_typeField.Type.ParseLiteral(field.Value);
                        break;
                }
            }

            if (coordinates == null || type != GeoJSONGeometryType.Point)
            {
                throw new InputObjectSerializationException(
                    "Failed to serialize PointInputObject. You have to at least specify x and y");
            }

            // var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid.Value);

            return new Point(coordinates);
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
            _coordinatesField = Fields[_coordinatesFieldName];
            _typeField = Fields[_typeFieldName];
        }
    }
}
