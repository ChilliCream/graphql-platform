using System.Collections.Generic;
using GeoAPI.CoordinateSystems;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using NetTopologySuite.Geometries;
using Types.Spatial.Common;

namespace Types.Spatial.Output
{
    public enum CRSType {
        Link,
        Name
    }

    public enum CRSLinkType {
        Proj4,
        OgcWKT,
        EsriWKT
    }

    public class CRSProperties {
        public string? Href { get; set; }
        public CRSLinkType Type { get; set; }
        public string? Name { get; set; }
    }

    public class CRS
    {
        public CRSType Type { get; set; }
        public CRSProperties? Properties { get; set; }
    }
