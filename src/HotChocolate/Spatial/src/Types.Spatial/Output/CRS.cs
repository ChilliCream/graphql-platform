using System.Collections.Generic;
using GeoAPI.CoordinateSystems;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using NetTopologySuite.Geometries;
using Types.Spatial.Common;

namespace Types.Spatial.Output
{
    /// https://geojson.org/geojson-spec.html#coordinate-reference-system-objects
    // "crs": { "type": "name", "properties": { "name": "urn:ogc:def:crs:EPSG::26912" } }
    public enum CRSType
    {
        Name
    }

    public class CRSProperties
    {
        public string? Name { get; set; }
    }

    public class CRS
    {
        public CRSType Type { get; set; }
        public CRSProperties? Properties { get; set; }
    }
}
