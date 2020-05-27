using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONMultiPolygonInput : InputObjectType<MultiPolygon>
    {
        private const string _typeFieldName = "type";
        private const string _coordinatesFieldName = "coordinates";
        private const string _crsFieldName = "crs";
        private const GeoJSONGeometryType _geometryType = GeoJSONGeometryType.MultiPolygon;
        private IInputField _typeField = default!;
        private GeoJSONGeometryType _typeFieldType = default!;
        private IInputField _coordinatesField = default!;
        private IInputField _crsField = default!;

        protected override void Configure(IInputObjectTypeDescriptor<MultiPolygon> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(_typeFieldName)
                .Type<EnumType<GeoJSONGeometryType>>();
            descriptor.Field(_coordinatesFieldName)
                .Type<ListType<ListType<GeoJSONPositionScalar>>>();
            descriptor.Field(_crsFieldName)
                .Type<IntType>();
        }

        public override object? ParseLiteral(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return null;
            }

            if (!(literal is ObjectValueNode obj) || obj.Fields.Count < 2)
            {
                throw ThrowHelper.InvalidInputObjectStructure(_geometryType);
            }

            (int typeIndex, int coordinateIndex, int crsIndex) indices = 
                ParseLiteralHelper.GetFieldIndices(obj,
                    _typeFieldName,
                    _coordinatesFieldName,
                    _crsFieldName);

            if (indices.typeIndex == -1)
            {
                throw ThrowHelper.InvalidInputObjectStructure(_geometryType);
            }

            var type = (GeoJSONGeometryType)
                _typeField.Type.ParseLiteral(obj.Fields[indices.typeIndex].Value);

            if (type != _geometryType || indices.coordinateIndex == -1)
            {
                throw ThrowHelper.InvalidInputObjectStructure(_geometryType);
            }

            var parts = (List<List<Coordinate>>)
                _coordinatesField.Type.ParseLiteral(obj.Fields[indices.coordinateIndex].Value);

            if (parts.Count < 1)
            {
                throw ThrowHelper.InvalidInputObjectStructure(_geometryType);
            }

            var polygonCount = parts.Count;
            var geometries = new Polygon[polygonCount];

            for (var i = 0; i < polygonCount; i++)
            {
                var pointCount = parts[i].Count;
                var coordinates = new Coordinate[pointCount];

                for (var j = 0; j < pointCount; j++)
                {
                    coordinates[j] = new Coordinate(parts[i][j]);
                }

                var ring = new LinearRing(coordinates);

                geometries[i] = new Polygon(ring);

            }

            if (indices.crsIndex == -1)
            {
                return new MultiPolygon(geometries);
            }

            var srid = (int)_crsField.Type.ParseLiteral(obj.Fields[indices.crsIndex].Value);

            GeometryFactory factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid);

            return factory.CreateMultiPolygon(geometries);
        }

        protected override void OnAfterCompleteType(
            ICompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            _coordinatesField = Fields[_coordinatesFieldName];
            _typeField = Fields[_typeFieldName];
            _crsField = Fields[_crsFieldName];
        }
    }
}
