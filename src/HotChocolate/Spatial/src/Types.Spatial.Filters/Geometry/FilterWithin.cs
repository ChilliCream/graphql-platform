using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Filters
{
    public class FilterWithin
    {
        public FilterWithin(Geometry shape)
        {
            Shape = shape;
        }

        public Geometry Shape { get; }
    }
}
