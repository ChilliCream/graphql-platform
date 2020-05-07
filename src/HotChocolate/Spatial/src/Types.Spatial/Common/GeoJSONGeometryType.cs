namespace Types.Spatial.Common
{
    public enum GeoJSONGeometryType
    {
        Point,
        MultiPoint,
        LineString,
        MultiLineString,
        Polygon,
        MultiPolygon,
        GeometryCollection,
    }

    // https://tools.ietf.org/html/rfc7946#section-1.4
    public enum GeoJSONTypes {
        Feature,
        FeatureCollection
    }
}
