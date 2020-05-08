using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Playground
{
    [ExtendObjectType(Name = "Mutation")]
    public class GeoMutations
    {
        public Point CreatePoint(Coordinate coordinates)
        {
            var p = new Point(coordinates);

            return p;
        }

        public Coordinate CreateCoordinate(Coordinate coordinates)
        {
            return new Coordinate(coordinates.X, coordinates.Y);
        }

        public Coordinate CreateCoordinateZ(Coordinate coordinates)
        {
            return new Coordinate(coordinates.X, coordinates.Y) {Z = coordinates.Z};
        }
    }
}
