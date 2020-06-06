using NetTopologySuite.Geometries;
using HotChocolate.Types.Filters;

namespace HotChocolate.Types.Spatial.Filters
{
    public class FilterDistance
    {
        public FilterDistance(Point shape)
        {
            Shape = shape;
        }

        public Point Shape { get; }

        [FilterMetaField]
        public double Is { get; set; }
    }
}
