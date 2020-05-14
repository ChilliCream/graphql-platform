using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONMultiLineStringInput : InputObjectType<MultiLineString>
    {
        private const string _typeFieldName = "type";
        private const string _coordinatesFieldName = "coordinates";
        private const GeoJSONGeometryType _geometryType = GeoJSONGeometryType.MultiLineString;
        private IInputField _typeField = default!;
        private IInputField _coordinatesField = default!;

        protected override void Configure(IInputObjectTypeDescriptor<MultiLineString> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(_typeFieldName).Type<EnumType<GeoJSONGeometryType>>();
            descriptor.Field(_coordinatesFieldName).Type<ListType<ListType<GeoJSONPositionScalar>>>();
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

            var parts = (List<List<Coordinate>>)
                _coordinatesField.Type.ParseLiteral(obj.Fields[indices.coordinateIndex].Value);

            if (parts is null || parts.Count < 1)
            {
                ThrowHelper.InvalidInputObjectStructure(_geometryType);

                return null;
            }

            var lineCount = parts.Count;
            var geometries = new LineString[lineCount];

            for (var i = 0; i < lineCount; i++)
            {
                var pointCount = parts[i].Count;
                var coordinates = new Coordinate[pointCount];

                for (var j = 0; j < pointCount; j++)
                {
                    coordinates[j] = new Coordinate(parts[i][j]);
                }

                geometries[i] = new LineString(coordinates);
            }

            return new MultiLineString(geometries);
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
