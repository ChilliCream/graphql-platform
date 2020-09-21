using System.Collections.Generic;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial
{
    internal class GeoJsonResolvers
    {
        public GeoJsonGeometryType GetType([Parent] Geometry geometry)
        {
            return geometry.OgcGeometryType switch
            {
                OgcGeometryType.Point => GeoJsonGeometryType.Point,
                OgcGeometryType.LineString => GeoJsonGeometryType.LineString,
                OgcGeometryType.Polygon => GeoJsonGeometryType.Polygon,
                OgcGeometryType.MultiPoint => GeoJsonGeometryType.MultiPoint,
                OgcGeometryType.MultiLineString => GeoJsonGeometryType.MultiLineString,
                OgcGeometryType.MultiPolygon => GeoJsonGeometryType.MultiPolygon,
                _ => throw Resolver_Type_InvalidGeometryType()
            };
        }

        public IReadOnlyCollection<double> GetBbox([Parent] Geometry geometry)
        {
            Envelope envelope = geometry.EnvelopeInternal;

            // TODO: support Z
            return new[] { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY };
        }

        public int GetCrs([Parent] Geometry geometry)
        {
            if (geometry.SRID == 0)
            {
                return 4326;
            }

            return geometry.SRID;
        }
    }
}
