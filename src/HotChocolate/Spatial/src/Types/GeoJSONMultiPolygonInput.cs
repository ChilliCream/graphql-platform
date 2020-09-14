using System.Collections.Generic;
using HotChocolate.Language;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Properties.Resources;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONMultiPolygonInput
        : GeoJSONInputObjectType<MultiPolygon>
    {
        public override GeoJSONGeometryType GeometryType => GeoJSONGeometryType.MultiPolygon;

        protected override void Configure(IInputObjectTypeDescriptor<MultiPolygon> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(TypeFieldName)
                .Type<EnumType<GeoJSONGeometryType>>()
                .Description(GeoJSON_Field_Type_Description);
            descriptor.Field(CoordinatesFieldName)
                .Type<ListType<ListType<GeoJSONPositionScalar>>>()
                .Description(GeoJSON_Field_Coordinates_Description_MultiPolygon);
            descriptor.Field(CrsFieldName)
                .Type<IntType>()
                .Description(GeoJSON_Field_Crs_Description);
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

            if (TryParseCrs(obj, indices.crsIndex, out var srid))
            {
                GeometryFactory factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid);
                return factory.CreateMultiPolygon(geometries);
            }

            return new MultiPolygon(geometries);
        }
    }
}
