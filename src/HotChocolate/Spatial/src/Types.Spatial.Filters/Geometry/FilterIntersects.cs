using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Filters
{
    public class FilterIntersects
    {
        public FilterIntersects(Geometry shape)
        {
            Shape = shape;
        }

        public Geometry Shape { get; }
    }
}
