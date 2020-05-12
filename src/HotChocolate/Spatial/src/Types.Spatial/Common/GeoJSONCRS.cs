using HotChocolate;
using HotChocolate.Types;
using NetTopologySuite.Geometries;
using Types.Spatial.Output;

namespace Types.Spatial.Common
{
    public class CrsResolvers
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
