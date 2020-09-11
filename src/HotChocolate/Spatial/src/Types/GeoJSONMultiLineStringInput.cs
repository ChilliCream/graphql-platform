using System.Collections.Generic;
using HotChocolate.Language;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Properties.Resources;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONMultiLineStringInput
        : GeoJSONInputObjectType<MultiLineString>
    {
        public override GeoJSONGeometryType GeometryType => GeoJSONGeometryType.MultiLineString;

        protected override void Configure(IInputObjectTypeDescriptor<MultiLineString> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(TypeFieldName)
                .Type<EnumType<GeoJSONGeometryType>>()
                .Description(GeoJSON_Field_Type_Description);
            descriptor.Field(CoordinatesFieldName)
                .Type<ListType<ListType<GeoJSONPositionScalar>>>()
                .Description(GeoJSON_Field_Coordinates_Description_MultiLineString);
            descriptor.Field(CrsFieldName)
                .Type<IntType>()
                .Description(GeoJSON_Field_Crs_Description);
            ;
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            valueSyntax.EnsureObjectValueNode(out var obj);

            var indices = GetFieldIndices(obj);

            ValidateGeometryKind(obj, indices.typeIndex);

            IList<List<Coordinate>> parts = ParseCoordinateParts(obj, indices.coordinateIndex, 1);

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

            if (TryParseCrs(obj, indices.crsIndex, out var srid))
            {
                GeometryFactory factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid);
                return factory.CreateMultiLineString(geometries);
            }

            return new MultiLineString(geometries);
        }
    }
}
