namespace HotChocolate.Types.Spatial
{
    [GraphQLName("GeoJSONGeometryType")]
    public enum GeoJsonGeometryType
    {
        Point,
        MultiPoint,
        LineString,
        MultiLineString,
        Polygon,
        MultiPolygon,
        GeometryCollection,
    }
}
