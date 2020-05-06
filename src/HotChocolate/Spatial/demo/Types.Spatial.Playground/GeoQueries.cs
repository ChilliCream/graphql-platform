using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Playground
{
    [ExtendObjectType(Name = "Query")]
    public class GeoQueries
    {
        public double GetPointX(Point point)
        {
            return point.X;
        }

        public int GetPointSRID(Point point)
        {
            return point.SRID;
        }

        public int GetLineCount(LineString line)
        {
            return line.NumPoints;
        }

        public double GetLineLength(LineString line)
        {
            return line.Length;
        }
    }
}
