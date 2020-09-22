using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Spatial.Filters.Tests
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
            schema.ToString().MatchSnapshot();
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
