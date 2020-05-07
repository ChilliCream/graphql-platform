using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Playground
{
    [ExtendObjectType(Name = "Query")]
    public class GeoQueries
    {
        /*public double GetPointX(Point point)
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

        public Point GetPointRaw(Point point)
        {
            return point;
        }*/

        public Point GetPointRaw()
        {
            return new Point(1.1, 2.1);
        }
    }
}
