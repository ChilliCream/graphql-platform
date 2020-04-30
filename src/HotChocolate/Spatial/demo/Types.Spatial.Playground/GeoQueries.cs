using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Playground
{
    [ExtendObjectType(Name = "Query")]
    public class GeoQueries
    {
        public double GetPointX(Point p)
        {
            return p.X;
        }
    }
}
