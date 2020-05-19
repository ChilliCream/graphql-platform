using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONResolvers
    {
        public GeoJSONGeometryType GetType([Parent] Geometry geometry) {
            return geometry.OgcGeometryType switch
            {
                OgcGeometryType.Point => GeoJSONGeometryType.Point,
                OgcGeometryType.LineString => GeoJSONGeometryType.LineString,
                OgcGeometryType.Polygon => GeoJSONGeometryType.Polygon,
                OgcGeometryType.MultiPoint => GeoJSONGeometryType.MultiPoint,
                OgcGeometryType.MultiLineString => GeoJSONGeometryType.MultiLineString,
                OgcGeometryType.MultiPolygon => GeoJSONGeometryType.MultiPolygon,
                OgcGeometryType.GeometryCollection => throw new NotImplementedException(
                    "Geometry Collection is not supported yet"),
                _ => throw new ArgumentException("Geometry type is not supported"),
            };
        }

        public IReadOnlyCollection<double> GetBbox([Parent] Geometry geometry)
        {
            var envelope = geometry.EnvelopeInternal;

            // TODO: support Z
            return new[] { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY };
        }

        public int GetCrs([Parent] Geometry geometry)
        {
            if (geometry.SRID == 0) {
                return 4326;
            }

            return geometry.SRID;
        }
    }
}
