using HotChocolate.Language;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Properties.Resources;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONPointInput : GeoJSONInputObjectType<Point>
    {
        public override GeoJSONGeometryType GeometryType => GeoJSONGeometryType.Point;

        protected override void Configure(IInputObjectTypeDescriptor<Point> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(TypeFieldName)
                .Type<EnumType<GeoJSONGeometryType>>()
                .Description(GeoJSON_Field_Type_Description);
            descriptor.Field(CoordinatesFieldName)
                .Type<GeoJSONPositionScalar>()
                .Description(GeoJSON_Field_Coordinates_Description_Point);
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

            Coordinate coordinates = ParsePoint(obj, indices.coordinateIndex);

            if (TryParseCrs(obj, indices.crsIndex, out var srid))
            {
                GeometryFactory factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid);
                return factory.CreatePoint(coordinates);
            }

            return new Point(coordinates);
        }
    }
}
