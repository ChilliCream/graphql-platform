using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Filters
{
    public class FilterDistance
    {
        public FilterDistance(Point shape)
        {
            Shape = shape;
        }

        public Point Shape { get; }

        public double Is { get; set; }
    }
}
