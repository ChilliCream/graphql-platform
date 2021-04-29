using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionVisitorExecutableTests
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = true, Baz = "a" },
            new Foo { Bar = false, Baz = "b" }
        };

        private static Author[] _authors { get; } =
        {
            new() { Id = 1, Name = "Foo", Books = { new Book { Id = 1, Title = "Foo1" } } },
            new() { Id = 2, Name = "Bar", Books = { new Book { Id = 2, Title = "Bar1" } } },
            new()
            {
                Id = 3,
                Name = "Baz",
                Books =
                {
                    new Book { Id = 3, Title = "Baz1" }, new Book { Id = 4, Title = "Baz2" }
                }
            }
        };

        private readonly SchemaCache _cache = new SchemaCache();

        [Fact]
        public async Task Create_ProjectsTwoProperties_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable{ bar baz }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsOneProperty_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable{ baz }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsOneProperty_WithResolver()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                objectType: new ObjectType<Foo>(
                    x => x
                        .Field("foo")
                        .Resolver(new[]
                        {
                            "foo"
                        })
                        .Type<ListType<StringType>>()));

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable{ baz foo }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ResultBooks_When_NestedFiltering()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_authors);

            // act
            IExecutionResult result = await tester.ExecuteAsync(
                @"
                {
                    root(where: {name: {eq: ""Baz""}}) {
                        name
                        books(where: {title: {eq: ""Baz1"" } }, order: {title: DESC}) {
                            title
                        }
                    }
                }
                ");

            // assert
            result.MatchSqlSnapshot();
        }

        public class Foo
        {
            public int Id { get; set; }

            public bool Bar { get; set; }

            public string Baz { get; set; }
        }

        public class Author
        {
            public int Id { get; set; }

            public string? Name { get; set; }

            [UseFiltering]
            [UseSorting]
            public virtual ICollection<Book> Books { get; set; } =
                new List<Book>();
        }

        public class Book
        {
            public int Id { get; set; }

            public int AuthorId { get; set; }

            public string? Title { get; set; }

            public virtual Author? Author { get; set; }
        }
    }
}
