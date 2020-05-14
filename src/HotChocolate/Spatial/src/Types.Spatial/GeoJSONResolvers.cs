using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONResolvers
    {
        public GeoJSONCoordinateReferenceSystem GetCrs([Parent] Geometry geometry)
        {
            return new GeoJSONCoordinateReferenceSystem
            {
                Type = CRSType.Name,
                Properties = new CRSProperties {
                    Name = "urn:ogc:def:crs:OGC::CRS84"
                }
            };
        }

        public float GetSrid([Parent] Geometry geometry)
        {
            if (geometry.SRID == 0) {
                return 4326;
            }

            return geometry.SRID;
        }
    }
}
