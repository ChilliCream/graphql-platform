using NetTopologySuite.Geometries;
using HotChocolate.Types.Filters;

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

        [FilterMetaField]
        public double Is { get; set; }
    }
}