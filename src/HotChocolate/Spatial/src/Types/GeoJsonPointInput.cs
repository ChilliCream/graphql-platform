using HotChocolate.Language;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Properties.Resources;

namespace HotChocolate.Types.Spatial
{
    public class GeoJsonPointInput : GeoJsonInputObjectType<Point>
    {
        public override GeoJsonGeometryType GeometryType => GeoJsonGeometryType.Point;

        protected override void Configure(IInputObjectTypeDescriptor<Point> descriptor)
        {
            descriptor.GeoJsonName(nameof(GeoJsonPointInput));

            descriptor.BindFieldsExplicitly();

            descriptor.Field(TypeFieldName)
                .Type<EnumType<GeoJsonGeometryType>>()
                .Description(GeoJson_Field_Type_Description);
            descriptor.Field(CoordinatesFieldName)
                .Type<GeoJsonPositionType>()
                .Description(GeoJson_Field_Coordinates_Description_Point);
            descriptor.Field(CrsFieldName)
                .Type<IntType>()
                .Description(GeoJson_Field_Crs_Description);
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
