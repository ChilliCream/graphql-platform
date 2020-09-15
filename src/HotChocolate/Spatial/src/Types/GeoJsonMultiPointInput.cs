using System.Collections.Generic;
using HotChocolate.Language;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Properties.Resources;

namespace HotChocolate.Types.Spatial
{
    public class GeoJsonMultiPointInput : GeoJsonInputObjectType<MultiPoint>
    {
        public override GeoJsonGeometryType GeometryType => GeoJsonGeometryType.MultiPoint;

        protected override void Configure(IInputObjectTypeDescriptor<MultiPoint> descriptor)
        {
            descriptor.GeoJsonName(nameof(GeoJsonMultiPointInput));

            descriptor.BindFieldsExplicitly();

            descriptor.Field(TypeFieldName)
                .Type<EnumType<GeoJsonGeometryType>>()
                .Description(GeoJson_Field_Type_Description);
            descriptor.Field(CoordinatesFieldName)
                .Type<ListType<GeoJsonPositionType>>()
                .Description(GeoJson_Field_Coordinates_Description_MultiPoint);
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

            IList<Coordinate> coordinates = ParseCoordinateValues(obj, indices.coordinateIndex, 1);

            var pointCount = coordinates.Count;
            var points = new Point[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                points[i] = new Point(coordinates[i]);
            }

            if (TryParseCrs(obj, indices.crsIndex, out var srid))
            {
                GeometryFactory factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid);
                return factory.CreateMultiPoint(points);
            }

            return new MultiPoint(points);
        }
    }
}
