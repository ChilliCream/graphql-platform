using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters.Spatial.Tests
{
    public class FilterInputTypeTest
        : FilterTestBase
    {
        [Fact]
        public void GeometryFilterType_Native()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType<GeometryFilterType>());

            // assert
#if NETCOREAPP2_1
            schema.Print().MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            schema.Print().MatchSnapshot();
#endif
        }

        [Fact]
        public void LineStringFilterType_Native()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType<LineStringFilterType>());

            // assert
#if NETCOREAPP2_1
            schema.Print().MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            schema.Print().MatchSnapshot();
#endif
        }

        [Fact]
        public void MultiLineStringFilterType_Native()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType<MultiLineStringFilterType>());

            // assert
#if NETCOREAPP2_1
            schema.Print().MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            schema.Print().MatchSnapshot();
#endif
        }

        [Fact]
        public void PointFilterType_Native()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType<PointFilterType>());

            // assert
#if NETCOREAPP2_1
            schema.Print().MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            schema.Print().MatchSnapshot();
#endif
        }

        [Fact]
        public void MultiPointFilterType_Native()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType<MultiPointFilterType>());

            // assert
#if NETCOREAPP2_1
            schema.Print().MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            schema.Print().MatchSnapshot();
#endif
        }

        [Fact]
        public void PolygonFilterType_Native()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType<PolygonFilterType>());

            // assert
#if NETCOREAPP2_1
            schema.Print().MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            schema.Print().MatchSnapshot();
#endif
        }

        [Fact]
        public void MultiPolygonFilterType_Native()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType<MultiPolygonFilterType>());

            // assert
#if NETCOREAPP2_1
            schema.Print().MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            schema.Print().MatchSnapshot();
#endif
        }

        public class FooDirective
        {
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class Query
        {
            [GraphQLNonNullType]
            public IQueryable<Book> Books() => new List<Book>().AsQueryable();
        }

        public class Book
        {
            public int Id { get; set; }

            [GraphQLNonNullType]
            public string Title { get; set; }

            public int Pages { get; set; }
            public int Chapters { get; set; }

            [GraphQLNonNullType]
            public Author Author { get; set; }
        }

        public class Author
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public int Id { get; set; }

            [GraphQLNonNullType]
            public string Name { get; set; }
        }
    }
}
