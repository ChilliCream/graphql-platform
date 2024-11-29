using HotChocolate.Data.Filters.Spatial;
using HotChocolate.Data.Filters.Spatial.Tests;
using HotChocolate.Types;

namespace HotChocolate.Data.Spatial;

public class GeometryFilterInputTypeTest
    : FilterTestBase
{
    [Fact]
    public void GeometryFilterType_Native()
    {
        // arrange
        // act
        var schema = CreateSchema(s => s.AddType<GeometryFilterInputType>());

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void LineStringFilterType_Native()
    {
        // arrange
        // act
        var schema = CreateSchema(s => s.AddType<LineStringFilterInputType>());

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void MultiLineStringFilterType_Native()
    {
        // arrange
        // act
        var schema = CreateSchema(s => s.AddType<MultiLineStringFilterInputType>());

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void PointFilterType_Native()
    {
        // arrange
        // act
        var schema = CreateSchema(s => s.AddType<PointFilterInputType>());

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void MultiPointFilterType_Native()
    {
        // arrange
        // act
        var schema = CreateSchema(s => s.AddType<MultiPointFilterInputType>());

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void PolygonFilterType_Native()
    {
        // arrange
        // act
        var schema = CreateSchema(s => s.AddType<PolygonFilterInputType>());

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void MultiPolygonFilterType_Native()
    {
        // arrange
        // act
        var schema = CreateSchema(s => s.AddType<MultiPolygonFilterInputType>());

        // assert
        schema.MatchSnapshot();
    }

    public class FooDirective
    {
    }

    public class Foo
    {
        public string Bar { get; set; } = default!;
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
        public string Title { get; set; } = default!;

        public int Pages { get; set; }
        public int Chapters { get; set; }

        [GraphQLNonNullType]
        public Author Author { get; set; } = default!;
    }

    public class Author
    {
        [GraphQLType(typeof(NonNullType<IdType>))]
        public int Id { get; set; }

        [GraphQLNonNullType]
        public string Name { get; set; } = default!;
    }
}
