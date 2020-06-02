using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Filters
{
    public class FilterDistance
    {
        public FilterDistance(
            Point from)
        {
            From = from;
        }

        public Point From { get; }

        public double Is { get; set; }
    }
}