using System.Collections.Generic;
using HotChocolate.Types.Spatial.Properties;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONResolvers
    {
        public GeoJSONGeometryType GetType([Parent] Geometry geometry)
        {
            return geometry.OgcGeometryType switch
            {
                OgcGeometryType.Point => GeoJSONGeometryType.Point,
                OgcGeometryType.LineString => GeoJSONGeometryType.LineString,
                OgcGeometryType.Polygon => GeoJSONGeometryType.Polygon,
                OgcGeometryType.MultiPoint => GeoJSONGeometryType.MultiPoint,
                OgcGeometryType.MultiLineString => GeoJSONGeometryType.MultiLineString,
                OgcGeometryType.MultiPolygon => GeoJSONGeometryType.MultiPolygon,
                _ => throw Resolver_Type_InvalidGeometryType()
            };
        }

        public IReadOnlyCollection<double> GetBbox([Parent] Geometry geometry)
        {
            Envelope envelope = geometry.EnvelopeInternal;

            // TODO: support Z
            return new[] {envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY};
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
