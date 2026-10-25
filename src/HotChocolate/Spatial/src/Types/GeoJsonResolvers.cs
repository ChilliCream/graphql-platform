using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial;

internal class GeoJsonResolvers
{
    public Coordinate[][] GetGeometryCollectionCoordinates(
        [Parent] GeometryCollection collection)
    {
        var coordinates = new Coordinate[collection.Count][];
        for (var i = 0; i < collection.Count; i++)
        {
            coordinates[i] = collection[i].Coordinates;
        }

        return coordinates;
    }

    public Coordinate[][][] GetMultiPolygonCoordinates([Parent] MultiPolygon multiPolygon)
    {
        var coordinates = new Coordinate[multiPolygon.NumGeometries][][];

        for (var i = 0; i < multiPolygon.NumGeometries; i++)
        {
            coordinates[i] = GetPolygonRings((Polygon)multiPolygon.GetGeometryN(i));
        }

        return coordinates;
    }

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
        var envelope = geometry.EnvelopeInternal;

        // TODO: support Z
        return [envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY];
    }

    public int GetCrs([Parent] Geometry geometry) =>
        geometry.SRID is 0 or -1 ? 4326 : geometry.SRID;

    private static Coordinate[][] GetPolygonRings(Polygon polygon)
    {
        var rings = new Coordinate[polygon.NumInteriorRings + 1][];
        rings[0] = polygon.ExteriorRing.Coordinates;

        for (var i = 0; i < polygon.NumInteriorRings; i++)
        {
            rings[i + 1] = polygon.GetInteriorRingN(i).Coordinates;
        }

        return rings;
    }
}
