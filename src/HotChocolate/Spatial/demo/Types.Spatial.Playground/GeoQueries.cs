using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Playground
{
    [ExtendObjectType(Name = "Query")]
    public class GeoQueries
    {
        public Coordinate GetRawCoordinate()
        {
            return new Coordinate(1.0, 2.0);
        }

        public Coordinate GetRawCoordinateZ()
        {
            var coordinate = new Coordinate(1.0, 2.0) {Z = 3.0};

            return coordinate;
        }

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

        public LineString GetRawLineString()
        {
            return new LineString(new[] { new Coordinate(1.1, 2.1), new Coordinate(3.1, 5.1) });
        }

        public MultiLineString GetRawMultiLineString()
        {
            return new MultiLineString(new [] {
                new LineString(new[] { new Coordinate(1.1, 2.1), new Coordinate(3.1, 5.1) }),
                new LineString(new[] { new Coordinate(7.1, 2.1), new Coordinate(3.1, 7.1) })
            });
        }

        public Polygon GetRawPolygon()
        {
            return new Polygon(new LinearRing(new[] {
                new Coordinate(1.1, 2.1),
                new Coordinate(3.1, 5.1),
                new Coordinate(5.1, 7.1),
                new Coordinate(1.1, 2.1)
            }));
        }

        public MultiPolygon GetRawMultiPolygon()
        {
            return new MultiPolygon(new[] {
                new Polygon(new LinearRing(new[] {
                    new Coordinate(1.1, 2.1),
                    new Coordinate(3.1, 5.1),
                    new Coordinate(5.1, 7.1),
                    new Coordinate(1.1, 2.1)
                })),
                new Polygon(new LinearRing(new[] {
                    new Coordinate(5.1, 6.1),
                    new Coordinate(8.1, 9.1),
                    new Coordinate(10.1, 11.1),
                    new Coordinate(5.1, 6.1)
                }))
            });
        }

        public Point GetEchoCoord(Coordinate geom) {
            return new Point(geom);
        }

        public Point GetEchoPoint(Point geom)
        {
            return geom;
        }

        public MultiPoint GetEchoMultiPoint(MultiPoint geom)
        {
            return geom;
        }

        public LineString GetEchoLineString(LineString geom)
        {
            return geom;
        }

        public MultiLineString GetEchoMultiLineString(MultiLineString geom)
        {
            return geom;
        }

        public Polygon GetEchoPolygon(Polygon geom)
        {
            return geom;
        }

        public MultiPolygon GetEchoMultiPolygon(MultiPolygon geom)
        {
            return geom;
        }
    }
}
