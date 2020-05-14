using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONMultiPointInput : InputObjectType<MultiPoint>
    {
        private const string _typeFieldName = "type";
        private const string _coordinatesFieldName = "coordinates";
        private const GeoJSONGeometryType _geometryType = GeoJSONGeometryType.MultiPoint;
        private IInputField _typeField = default!;
        private IInputField _coordinatesField = default!;

        protected override void Configure(IInputObjectTypeDescriptor<MultiPoint> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(_typeFieldName).Type<EnumType<GeoJSONGeometryType>>();
            descriptor.Field(_coordinatesFieldName).Type<ListType<GeoJSONPositionScalar>>();
        }

        public override object? ParseLiteral(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return null;
            }

            if (!(literal is ObjectValueNode obj) || obj.Fields.Count < 2)
            {
                ThrowHelper.InvalidInputObjectStructure(_geometryType);

                return null;
            }

            (int typeIndex, int coordinateIndex) indices = ParseLiteralHelper.GetFieldIndices(obj,
                 _typeFieldName,
                 _coordinatesFieldName);

            if (indices.typeIndex == -1)
            {
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

            var coordinates = (IList<Coordinate>)
                _coordinatesField.Type.ParseLiteral(obj.Fields[indices.coordinateIndex].Value);

            if (coordinates is null || coordinates.Count < 1)
            {
                ThrowHelper.InvalidInputObjectStructure(_geometryType);

                return null;
            }

            // var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid.Value);
            var pointCount = coordinates.Count;
            var points = new Point[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                points[i] = new Point(coordinates[i]);
            }

            return new MultiPoint(points);
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
