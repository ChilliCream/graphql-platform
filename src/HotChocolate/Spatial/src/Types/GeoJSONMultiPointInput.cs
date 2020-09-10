using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HotChocolate.Spatial.Types
{
    public class GeoJSONMultiPointInput : InputObjectType<MultiPoint>
    {
        private const string _typeFieldName = "type";
        private const string _coordinatesFieldName = "coordinates";
        private const string _crsFieldName = "crs";
        private const GeoJSONGeometryType _geometryType = GeoJSONGeometryType.MultiPoint;
        private IInputField _typeField = default!;
        private IInputField _coordinatesField = default!;
        private IInputField _crsField = default!;

        protected override void Configure(IInputObjectTypeDescriptor<MultiPoint> descriptor)
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

            var coordinates = (IList<Coordinate>)
                _coordinatesField.Type.ParseLiteral(obj.Fields[indices.coordinateIndex].Value);

            if (coordinates.Count < 1)
            {
                throw ThrowHelper.InvalidInputObjectStructure(_geometryType);
            }

            var pointCount = coordinates.Count;
            var points = new Point[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                points[i] = new Point(coordinates[i]);
            }

            if (indices.crsIndex == -1)
            {
                return new MultiPoint(points);
            }

            var srid = (int)_crsField.Type.ParseLiteral(obj.Fields[indices.crsIndex].Value);
            GeometryFactory factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid);

            return factory.CreateMultiPoint(points);
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
