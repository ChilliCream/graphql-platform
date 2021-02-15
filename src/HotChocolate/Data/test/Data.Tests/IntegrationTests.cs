using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data
{
    public class IntegrationTests : IClassFixture<AuthorFixture>
    {
        private readonly Author[] _authors;

        public IntegrationTests(AuthorFixture authorFixture)
        {
            _authors = authorFixture.Authors;
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType(
                    x => x
                        .Name("Query")
                        .Field("executable")
                        .Resolve(_authors.AsExecutable())
                        .UseProjection()
                        .UseFiltering()
                        .UseSorting())
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType(
                    x => x
                        .Name("Query")
                        .Field("executable")
                        .Type<ObjectType<Author>>()
                        .Resolve(_authors.Take(1).AsExecutable())
                        .UseSingleOrDefault()
                        .UseProjection()
                        .UseFiltering()
                        .UseSorting())
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultMoreThanOne()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType(
                    x => x
                        .Name("Query")
                        .Field("executable")
                        .Type<ObjectType<Author>>()
                        .Resolve(_authors.AsExecutable())
                        .UseSingleOrDefault()
                        .UseProjection()
                        .UseFiltering()
                        .UseSorting())
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultZero()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType(
                    x => x
                        .Name("Query")
                        .Field("executable")
                        .Type<ObjectType<Author>>()
                        .Resolve(_authors.Take(0).AsExecutable())
                        .UseSingleOrDefault()
                        .UseProjection()
                        .UseFiltering()
                        .UseSorting())
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType(
                    x => x
                        .Name("Query")
                        .Field("executable")
                        .Type<ObjectType<Author>>()
                        .Resolve(_authors.AsExecutable())
                        .UseFirstOrDefault()
                        .UseProjection()
                        .UseFiltering()
                        .UseSorting())
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefaultZero()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType(
                    x => x
                        .Name("Query")
                        .Field("executable")
                        .Type<ObjectType<Author>>()
                        .Resolve(_authors.Take(0).AsExecutable())
                        .UseFirstOrDefault()
                        .UseProjection()
                        .UseFiltering()
                        .UseSorting())
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProjectAndPage_When_BothMiddlewaresAreApplied()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    books {
                        edges {
                            node {
                                id
                                author {
                                    name
                                }
                            }
                        }
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProjectAndPage_When_BothAreAppliedAndProvided()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    books {
                        nodes {
                            id
                        }
                        edges {
                            node {
                                title
                            }
                        }
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
<<<<<<< HEAD
        public async Task ExecuteAsync_Should_ArgumentAndFirstOrDefault_When_Executed()
=======
        public async Task ExecuteAsync_Should_ProjectAndPage_When_EdgesFragment()
>>>>>>> main
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
<<<<<<< HEAD
                .AddSorting()
                .AddProjections()
                .AddQueryType<FirstOrDefaulQuery>()
=======
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
>>>>>>> main
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
<<<<<<< HEAD
                    books(book: {id: 1}) {
                        title
                    }
                }
=======
                    books {
                        edges {
                            node {
                                ... Test
                            }
                        }
                    }
                }
                fragment Test on Book {
                    title
                    id
                }
>>>>>>> main
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
<<<<<<< HEAD
        public async Task
            Schema_Should_Generate_WhenMutationInputHasManyToManyRelationshipWithOutputType()
=======
        public async Task ExecuteAsync_Should_ProjectAndPage_When_NodesFragment()
>>>>>>> main
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
<<<<<<< HEAD
                .AddSorting()
                .AddProjections()
                .AddQueryType<FirstOrDefaulQuery>()
                .AddMutationType<FirstOrDefaultMutation_ManyToMany>()
                .BuildRequestExecutorAsync();

            // act
            var result = executor.Schema.Print();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task
            Schema_Should_Generate_WhenMutationInputHasManyToOneRelationshipWithOutputType()
=======
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    books {
                        nodes {
                            ... Test
                        }
                    }
                }
                fragment Test on Book {
                    title
                    id
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProjectAndPage_When_EdgesFragmentNested()
>>>>>>> main
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
<<<<<<< HEAD
                .AddSorting()
                .AddProjections()
                .AddQueryType<FirstOrDefaulQuery>()
                .AddMutationType<FirstOrDefaultMutation_ManyToOne>()
                .BuildRequestExecutorAsync();

            // act
            var result = executor.Schema.Print();

            // assert
            result.MatchSnapshot();
        }

        public class FirstOrDefaulQuery
        {
            [UseFirstOrDefault, UseProjection]
            public IQueryable<Book> GetBooks(Book book) => new[]
                {
                    new Book
                    {
                        Id = 1, Title = "BookTitle", Author = new Author { Name = "Author" }
                    },
                    new Book
                    {
                        Id = 2, Title = "BookTitle2", Author = new Author { Name = "Author2" }
                    }
                }.AsQueryable()
                .Where(x => x.Id == book.Id);
        }

        public class FirstOrDefaultMutation_ManyToMany
        {
            [UseFirstOrDefault, UseProjection]
            public IQueryable<Author> AddPublisher(Publisher publisher) => new[]
            {
                new Author { Name = "Author", Publishers = new List<Publisher> { publisher } }
            }.AsQueryable();
        }

        public class FirstOrDefaultMutation_ManyToOne
        {
            [UseFirstOrDefault, UseProjection]
            public IQueryable<Author> AddBook(Book book) => new[]
            {
                new Author { Name = "Author", Books = new List<Book> { book } }
            }.AsQueryable();
        }

=======
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    books {
                        edges {
                            node {
                                author {
                                   ... Test
                                }
                            }
                        }
                    }
                }
                fragment Test on Author {
                    name
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProjectAndPage_When_NodesFragmentNested()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    books {
                        nodes {
                            author {
                               ... Test
                            }
                        }
                    }
                }
                fragment Test on Author {
                    name
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }
        [Fact]
        public async Task ExecuteAsync_Should_ProjectAndPage_When_NodesFragmentContainsProjectedField()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    books {
                        nodes {
                            ... Test
                        }
                    }
                }
                fragment Test on Book {
                    title
                    author {
                       name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }
>>>>>>> main
        public class PagingAndProjection
        {
            [UsePaging]
            [UseProjection]
            public IQueryable<Book> GetBooks() => new[]
            {
                new Book { Id = 1, Title = "BookTitle", Author = new Author { Name = "Author" } }
            }.AsQueryable();
        }
    }
}
