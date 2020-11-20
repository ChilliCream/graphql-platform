using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Filters.Spatial
{
    public class PolygonFilterInputType
        : GeometryFilterInputType<Polygon>
    {
    }
}
