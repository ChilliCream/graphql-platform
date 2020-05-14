using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONLineStringInput : InputObjectType<LineString>
    {
        private const string _typeFieldName = "type";
        private const string _coordinatesFieldName = "coordinates";
        private const GeoJSONGeometryType _geometryType = GeoJSONGeometryType.LineString;
        private IInputField _typeField = default!;
        private IInputField _coordinatesField = default!;

        protected override void Configure(IInputObjectTypeDescriptor<LineString> descriptor)
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

            IList<Coordinate>? coordinates = null;
            GeoJSONGeometryType? type = null;

            for (var i = 0; i < obj.Fields.Count; i++)
            {
                ObjectFieldNode field = obj.Fields[i];

                switch (field.Name.Value)
                {
                    case _coordinatesFieldName:
                        coordinates =
                            (IList<Coordinate>)_coordinatesField.Type.ParseLiteral(field.Value);
                        break;
                    case _typeFieldName:
                        type = (GeoJSONGeometryType)_typeField.Type.ParseLiteral(field.Value);
                        break;
                }
            }

            if (type != _geometryType || coordinates is null || coordinates.Count < 2)
            {
                ThrowHelper.InvalidInputObjectStructure(_geometryType);

                return null;
            }

            // var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid.Value);
            var coords = new Coordinate[coordinates.Count];
            coordinates.CopyTo(coords, 0);

            return new LineString(coords);
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
