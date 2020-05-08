using HotChocolate;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Common
{
    public class CrsResolvers
    {
        public string GetCrs([Parent] Geometry geometry)
            => "urn:ogc:def:crs:OGC::CRS84";
    }
}
