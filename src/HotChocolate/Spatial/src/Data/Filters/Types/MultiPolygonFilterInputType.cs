using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Filters.Spatial
{
    public class MultiPolygonFilterInputType
        : GeometryFilterInputType<MultiPolygon>
    {
    }
}
