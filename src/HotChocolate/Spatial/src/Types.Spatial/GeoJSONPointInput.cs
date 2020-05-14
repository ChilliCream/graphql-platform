using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONPointInput : InputObjectType<Point>
    {
        private const string _typeFieldName = "type";
        private const string _coordinatesFieldName = "coordinates";
        private const GeoJSONGeometryType _geometryType = GeoJSONGeometryType.Point;
        private IInputField _typeField = default!;
        private IInputField _coordinatesField = default!;

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

            if (!(literal is ObjectValueNode obj))
            {
               ThrowHelper.InvalidInputObjectStructure(_geometryType);

                return null;
            }

            (int typeIndex, int coordinateIndex) indices = ParseLiteralHelper.GetFieldIndices(obj,
                _typeFieldName,
                _coordinatesFieldName);

            if (indices.typeIndex == -1) {
                ThrowHelper.InvalidInputObjectStructure(_geometryType);

                return null;
            }

            var type = (GeoJSONGeometryType)
                _typeField.Type.ParseLiteral(obj.Fields[indices.typeIndex].Value);

            if (type != _geometryType || indices.coordinateIndex == -1)
            {
                ThrowHelper.InvalidInputObjectStructure(_geometryType);

                return null;
            }

            var coordinates = (Coordinate)
                _coordinatesField.Type.ParseLiteral(obj.Fields[indices.coordinateIndex].Value);

            if (coordinates is null)
            {
                ThrowHelper.InvalidInputObjectStructure(_geometryType);

                return null;
            }

            // var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid.Value);

            return new Point(coordinates);
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
