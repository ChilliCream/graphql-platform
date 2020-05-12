namespace Types.Spatial.Output
{
    /// https://geojson.org/geojson-spec.html#coordinate-reference-system-objects
    // "crs": { "type": "name", "properties": { "name": "urn:ogc:def:crs:EPSG::26912" } }

    public class GeoJSONCoordinateReferenceSystem
    {
        public CRSType Type { get; set; }
        public CRSProperties? Properties { get; set; }
    }

    public class CRSProperties
    {
        public string? Name { get; set; }
    }

    public enum CRSType
    {
        Name
    }
}
