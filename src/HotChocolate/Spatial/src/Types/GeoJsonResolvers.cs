using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types.Spatial.Serialization;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial
{
    internal class GeoJsonResolvers
    {
        public GeoJsonGeometryType GetType([Parent] Geometry geometry) =>
            geometry.OgcGeometryType switch
            {
                OgcGeometryType.Point => GeoJsonGeometryType.Point,
                OgcGeometryType.LineString => GeoJsonGeometryType.LineString,
                OgcGeometryType.Polygon => GeoJsonGeometryType.Polygon,
                OgcGeometryType.MultiPoint => GeoJsonGeometryType.MultiPoint,
                OgcGeometryType.MultiLineString => GeoJsonGeometryType.MultiLineString,
                OgcGeometryType.MultiPolygon => GeoJsonGeometryType.MultiPolygon,
                _ => throw Resolver_Type_InvalidGeometryType()
            };

        public IReadOnlyCollection<double> GetBbox([Parent] Geometry geometry)
        {
            Envelope envelope = geometry.EnvelopeInternal;

            // TODO: support Z
            return new[] { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY };
        }

        public int GetCrs([Parent] Geometry geometry) =>
            geometry.SRID == 0 ? 4326 : geometry.SRID;

        public IReadOnlyCollection<double> GetPointCoordinates([Parent] Point point) =>
            ResolveCoordinate(point.Coordinate);

        public IReadOnlyCollection<IReadOnlyCollection<double>> GetLineStringCoordinates([Parent] LineString lineString) =>
            lineString.Coordinates.Select(ResolveCoordinate).ToList();

        private IReadOnlyCollection<double> ResolveCoordinate(Coordinate coordinate) =>
            double.IsNaN(coordinate.Z) ? new[] {coordinate.X, coordinate.Y} : new[] {coordinate.X, coordinate.Y, coordinate.Z};
    }
}
