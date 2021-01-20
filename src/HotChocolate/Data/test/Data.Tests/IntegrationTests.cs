using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
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
