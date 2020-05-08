using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Playground
{
    [ExtendObjectType(Name = "Query")]
    public class GeoQueries
    {
        public Point GetRawPoint()
        {
            return new Point(1.1, 2.1);
        }

        public Point GetRawPointWithElevation()
        {
            return new Point(1.1, 2.1, 100);
        }

        public MultiPoint GetRawMultiPoint()
        {
            return new MultiPoint(new [] { new Point(1.1, 2.1), new Point(3.1, 5.1) });
        }
    }
}
