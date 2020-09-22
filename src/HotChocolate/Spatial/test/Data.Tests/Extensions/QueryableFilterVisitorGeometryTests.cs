using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types.Spatial;
using NetTopologySuite.Geometries;
using Xunit;

namespace HotChocolate.Data.Spatial.Filters.Expressions
{
    public class QueryableFilterVisitorGeometryTests
        : FilterVisitorTestBase
    {
        [Fact]
        public void Create_Contains()
        {
            // arrange
            IValueNode value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"
                {
                    bar: {
                        contains: {
                            geometry: {
                                type: Point
                                coordinates: [20, 20]
                            }
                            eq: true
                        }
                    }
                }");
            ExecutorBuilder tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            Func<Foo, bool> func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new LineString(
                    new[]
                    {
                        new Coordinate(10, 20), new Coordinate(20, 20), new Coordinate(30, 20)
                    })
            };
            Assert.True(func(a));

            var b = new Foo
            {
                Bar = new LineString(
                    new[]
                    {
                        new Coordinate(10, 10), new Coordinate(20, 10), new Coordinate(30, 10)
                    })
            };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_Contains_Buffer()
        {
            // arrange
            IValueNode value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                @"
                {
                    bar: {
                        contains: {
                            geometry: {
                                type: Point
                                coordinates: [20, 20]
                            }
                            buffer: 10
                            eq: true
                        }
                    }
                }");
            ExecutorBuilder tester = CreateProviderTester(new FilterInputType<Foo>());

            // act
            Func<Foo, bool> func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                Bar = new LineString(
                    new[]
                    {
                        new Coordinate(10, 20), new Coordinate(20, 20), new Coordinate(30, 20)
                    })
            };
            Assert.True(func(a));

            var b = new Foo
            {
                Bar = new LineString(
                    new[]
                    {
                        new Coordinate(10, 10), new Coordinate(20, 10), new Coordinate(30, 10)
                    })
            };
            Assert.True(func(b));

            var c = new Foo
            {
                Bar = new LineString(
                    new[] { new Coordinate(10, 0), new Coordinate(20, 0), new Coordinate(30, 0) })
            };
            Assert.False(func(c));
        }

        public class Foo
        {
            [GraphQLType(typeof(GeometryType))]
            public Geometry Bar { get; set; }
        }

        public class FooNullable
        {
            [GraphQLType(typeof(GeometryType))]
            public Geometry Bar { get; set; }
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
