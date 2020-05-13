using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using NetTopologySuite.Geometries;

namespace Types.Spatial
{
    public class GeoJSONMultiPolygonInput : InputObjectType<MultiPolygon>
    {
        private const string _typeFieldName = "type";
        private const string _coordinatesFieldName = "coordinates";
        private IInputField _typeField = default!;
        private IInputField _coordinatesField = default!;

        public GeoJSONMultiPolygonInput() { }

        protected override void Configure(IInputObjectTypeDescriptor<MultiPolygon> descriptor)
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
                    "Failed to serialize MultiPolygon. Needs at least type and coordinates fields");
            }

            Coordinate[][]? parts = null;
            GeoJSONGeometryType? type = null;

            for (var i = 0; i < obj.Fields.Count; i++)
            {
                ObjectFieldNode field = obj.Fields[i];

                switch (field.Name.Value)
                {
                    case _coordinatesFieldName:
                        parts = (Coordinate[][])_coordinatesField.Type.ParseLiteral(field.Value);
                        break;
                    case _typeFieldName:
                        type = (GeoJSONGeometryType)_typeField.Type.ParseLiteral(field.Value);
                        break;
                }
            }

            if (parts == null || type != GeoJSONGeometryType.MultiPolygon)
            {
                throw new InputObjectSerializationException(
                    "Failed to serialize MultiPolygon. You have to at least specify a type and coordinates array");
            }

            // var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid.Value);
            var geometries = new Polygon[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                var coordinates = new Coordinate[parts[i].Length];
                for (var j = 0; j < parts[i].Length; j++)
                {
                    coordinates[j] = new Coordinate(parts[i][j]);
                }

                var ring = new LinearRing(coordinates);
                geometries[i] = new Polygon(ring);
            }

            return new MultiPolygon(geometries);
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
