using static HotChocolate.Types.Spatial.GeoJsonGeometryType;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial
{
    public sealed class GeoJsonGeometryEnumType : EnumType<GeoJsonGeometryType>
    {
        protected override void Configure(IEnumTypeDescriptor<GeoJsonGeometryType> descriptor)
        {
            descriptor.BindValuesExplicitly();

            descriptor.Name(EnumTypeTypeName);

            descriptor.Value(Point).Name(nameof(Point));
            descriptor.Value(MultiPoint).Name(nameof(MultiPoint));
            descriptor.Value(LineString).Name(nameof(LineString));
            descriptor.Value(MultiLineString).Name(nameof(MultiLineString));
            descriptor.Value(Polygon).Name(nameof(Polygon));
            descriptor.Value(MultiPolygon).Name(nameof(MultiPolygon));
            descriptor.Value(GeometryCollection).Name(nameof(GeometryCollection));
        }
    }
}
