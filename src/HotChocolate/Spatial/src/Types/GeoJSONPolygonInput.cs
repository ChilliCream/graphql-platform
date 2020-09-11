using System.Collections.Generic;
using HotChocolate.Language;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Properties.Resources;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONPolygonInput : GeoJSONInputObjectType<Polygon>
    {
        public override GeoJSONGeometryType GeometryType => GeoJSONGeometryType.Polygon;

        protected override void Configure(IInputObjectTypeDescriptor<Polygon> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(TypeFieldName)
                .Type<EnumType<GeoJSONGeometryType>>()
                .Description(GeoJSON_Field_Type_Description);
            descriptor.Field(CoordinatesFieldName)
                .Type<ListType<GeoJSONPositionScalar>>()
                .Description(GeoJSON_Field_Coordinates_Description_Polygon);
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

            IList<Coordinate> coordinates = ParseCoordinateValues(obj, indices.coordinateIndex, 4);

            var coords = new Coordinate[coordinates.Count];
            coordinates.CopyTo(coords, 0);

            var ring = new LinearRing(coords);

            if (TryParseCrs(obj, indices.crsIndex, out var srid))
            {
                GeometryFactory factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid);
                return factory.CreatePolygon(ring);
            }

            return new Polygon(ring);
        }
    }
}
