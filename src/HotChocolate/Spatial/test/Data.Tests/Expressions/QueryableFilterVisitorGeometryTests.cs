using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Spatial.Expressions;
using HotChocolate.Language;
using HotChocolate.Types.Spatial;
using NetTopologySuite.Geometries;
using static HotChocolate.Data.Spatial.Expressions.QueryableFilterVisitorGeometryTests.TestModels;

namespace HotChocolate.Data.Spatial.Expressions;

public static class QueryableFilterVisitorGeometryTests
{
    public class ContainsTests : FilterVisitorTestBase
    {
        [Fact]
        public void Line_Contains_Point()
        {
            // arrange
            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"{
                        bar: {
                            contains: {
                                geometry: {
                                    type: Point
                                    coordinates: [20, 20]
                                }
                            }
                        }
                    }");
            var tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            var func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new LineString(
                [
                    new Coordinate(10, 20),
                        new Coordinate(20, 20),
                        new Coordinate(30, 20),
                ]),
            };
            Assert.True(func(a));

            var b = new Foo
            {
                Bar = new LineString(
                [
                    new Coordinate(10, 10),
                        new Coordinate(20, 10),
                        new Coordinate(30, 10),
                ]),
            };
            Assert.False(func(b));
        }

        [Fact]
        public void Polygon_Contains_Buffered_Point()
        {
            // arrange
            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"{
                        bar: {
                            contains: {
                                geometry: {
                                    type: Point
                                    coordinates: [3, 3]
                                }
                                buffer: 2
                            }
                        }
                    }");

            var tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            var func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(0, 6),
                        new Coordinate(6, 6),
                        new Coordinate(6, 0),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.True(func(a), "polygon a does not contain the buffered point");

            var b = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(0, 6),
                        new Coordinate(6, 6),
                        new Coordinate(4, 4),
                        new Coordinate(6, 0),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.False(func(b), "polygon c contains the buffered point");
        }
    }

    public class DistanceTests : FilterVisitorTestBase
    {
        // https://github.com/NetTopologySuite/NetTopologySuite/blob/
        // d0dde923299674e3320e256775b1336e72379e2b/test/NetTopologySuite.
        // Tests.NUnit/Algorithm/DistanceComputerTest.cs#L22-L28
        [Fact]
        public void Point_to_Line()
        {
            // arrange
            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"{
                        bar: {
                            distance: {
                                geometry: {
                                    type: Point
                                    coordinates: [1, 1]
                                }
                                eq: 1
                            }
                        }
                    }");
            var tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            var func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new LineString(
                [
                    new Coordinate(2, 0),
                        new Coordinate(0, 0),
                        new Coordinate(1, 0),
                ]),
            };
            Assert.True(func(a));

            var b = new Foo
            {
                Bar = new LineString(
                [
                    new Coordinate(0.5, 0.5),
                        new Coordinate(0, 0),
                        new Coordinate(1, 0),
                ]),
            };
            Assert.False(func(b));
        }

        [Fact]
        public void Line_to_Line()
        {
            // arrange
            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"{
                        bar: {
                            distance: {
                                geometry: {
                                    type: LineString
                                    coordinates: [[0, 1], [1, 1], [2, 1]]
                                }
                                eq: 1
                            }
                        }
                    }");
            var tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            var func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new LineString(
                [
                    new Coordinate(0, 0),
                        new Coordinate(1, 0),
                ]),
            };
            Assert.True(func(a));
        }
    }

    public class IntersectTests : FilterVisitorTestBase
    {
        [Fact]
        public void Point_in_Poly()
        {
            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"{
                        bar: {
                            intersects: {
                                geometry: {
                                    type: Point
                                    coordinates: [1, 1]
                                }
                            }
                        }
                    }");
            var tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            var func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(1, 2),
                        new Coordinate(2, 0),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.True(func(a));

            var b = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(1, -2),
                        new Coordinate(2, 0),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.False(func(b));
        }

        [Fact]
        public void Line_in_Poly()
        {
            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"{
                        bar: {
                            intersects: {
                                geometry: {
                                    type: LineString
                                    coordinates: [[1, 1], [3, 1]]
                                }
                            }
                        }
                    }");
            var tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            var func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(1, 2),
                        new Coordinate(2, 0),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.True(func(a));

            var b = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(1, -2),
                        new Coordinate(2, 0),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.False(func(b));
        }

        [Fact]
        public void Poly_in_Poly()
        {
            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"{
                        bar: {
                            intersects: {
                                geometry: {
                                    type: Polygon
                                    coordinates: [[[1, 1], [3, 1], [2, 0], [1, 1]]]
                                }
                            }
                        }
                    }");
            var tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            var func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(1, 2),
                        new Coordinate(2, 0),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.True(func(a));

            var b = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(1, -2),
                        new Coordinate(2, -1),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.False(func(b));
        }
    }

    public class OverlapTests : FilterVisitorTestBase
    {
        [Fact]
        public void Line_and_Line()
        {
            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"{
                        bar: {
                            overlaps: {
                                geometry: {
                                    type: LineString
                                    coordinates: [[0, 0], [1, 2], [3, 1]]
                                }
                            }
                        }
                    }");
            var tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            var func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new LineString(
                [
                    new Coordinate(0, 0),
                        new Coordinate(1, 2),
                        new Coordinate(2, 0),
                ]),
            };
            Assert.True(func(a));

            var b = new Foo
            {
                Bar = new LineString(
                [
                    new Coordinate(0, 0),
                        new Coordinate(1, -2),
                        new Coordinate(2, 0),
                ]),
            };
            Assert.False(func(b));
        }

        [Fact]
        public void Poly_and_Poly()
        {
            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"{
                        bar: {
                            overlaps: {
                                geometry: {
                                    type: Polygon
                                    coordinates: [[[1, 1], [3, 1], [2, 0], [1, 1]]]
                                }
                            }
                        }
                    }");
            var tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            var func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(1, 2),
                        new Coordinate(2, 0),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.True(func(a));

            var b = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(1, -2),
                        new Coordinate(2, -1),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.False(func(b));
        }
    }

    public class WithinTests : FilterVisitorTestBase
    {
        [Fact]
        public void Point_Within_Line()
        {
            // arrange
            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"{
                        bar: {
                            within: {
                                geometry: {
                                    type: LineString
                                    coordinates: [[10, 20], [20, 20], [30, 20]]
                                }
                            }
                        }
                    }");
            var tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            var func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new Point(20, 20),
            };
            Assert.True(func(a));

            var b = new Foo
            {
                Bar = new Point(20, 30),
            };
            Assert.False(func(b));
        }

        [Fact]
        public void Polygon_Within_Buffered_Point()
        {
            // arrange
            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"{
                        bar: {
                            within: {
                                geometry: {
                                    type: Point
                                    coordinates: [3, 3]
                                }
                                buffer: 5
                            }
                        }
                    }");

            var tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            var func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(0, 2),
                        new Coordinate(2, 2),
                        new Coordinate(2, 0),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.True(func(a));

            var b = new Foo
            {
                Bar = new Polygon(new LinearRing(
                [
                    new Coordinate(0, 0),
                        new Coordinate(0, 9),
                        new Coordinate(9, 9),
                        new Coordinate(3, 3),
                        new Coordinate(9, 0),
                        new Coordinate(0, 0),
                ])),
            };
            Assert.False(func(b));
        }
    }

    public static class TestModels
    {
        public class Foo
        {
            [GraphQLType(typeof(GeometryType))]
            public Geometry? Bar { get; set; }
        }

        public class FooNullable
        {
            [GraphQLType(typeof(GeometryType))]
            public Geometry? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
        }
    }
}
