using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONPolygonInput : InputObjectType<Polygon>
    {
        private const string _typeFieldName = "type";
        private const string _coordinatesFieldName = "coordinates";
        private const string _crsFieldName = "crs";
        private const GeoJSONGeometryType _geometryType = GeoJSONGeometryType.Polygon;
        private IInputField _typeField = default!;
        private IInputField _coordinatesField = default!;
        private IInputField _crsField = default!;

        protected override void Configure(IInputObjectTypeDescriptor<Polygon> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(_typeFieldName).Type<EnumType<GeoJSONGeometryType>>();
            descriptor.Field(_coordinatesFieldName).Type<ListType<GeoJSONPositionScalar>>();
            descriptor.Field(_crsFieldName).Type<IntType>();
        }

        public override object? ParseLiteral(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return null;
            }

            if (!(literal is ObjectValueNode obj))
            {
                throw ThrowHelper.InvalidInputObjectStructure(_geometryType);
            }

            (int typeIndex, int coordinateIndex, int crsIndex) indices =
                ParseLiteralHelper.GetFieldIndices(
                    obj,
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

            var coordinates = (IList<Coordinate>)
                _coordinatesField.Type.ParseLiteral(obj.Fields[indices.coordinateIndex].Value);

            if (coordinates.Count < 4)
            {
                throw ThrowHelper.InvalidInputObjectStructure(_geometryType);
            }

            var coords = new Coordinate[coordinates.Count];
            coordinates.CopyTo(coords, 0);

            var ring = new LinearRing(coords);

            if (indices.crsIndex == -1)
            {
                return new Polygon(ring);
            }

            var srid = (int)_crsField.Type.ParseLiteral(obj.Fields[indices.crsIndex].Value);

            GeometryFactory factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid);

            return factory.CreatePolygon(ring);
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
