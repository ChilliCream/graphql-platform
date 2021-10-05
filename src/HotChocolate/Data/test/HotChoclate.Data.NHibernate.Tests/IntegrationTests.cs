
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data
{
    using System.Linq;

    [Collection("nhibernate-integration")]
    public class IntegrationTests 
    {
        private readonly IQueryable<Author> _authors;
        private readonly IQueryable<SingleOrDefaultAuthor> _singleOrDefaultAuthors;
        private readonly IQueryable<ZeroAuthor> _zeroAuthors;

        public IntegrationTests(AuthorFixture authorFixture)
        {
            _authors = authorFixture.Authors;
            _zeroAuthors = authorFixture.ZeroAuthor;
            _singleOrDefaultAuthors = authorFixture.SingleOrDefaultAuthor;
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
                        .Resolve(_authors.AsExecutable()))
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
                        .UseSingleOrDefault())
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
        public async Task ExecuteAsync_Should_ReturnNull_When_SingleOrDefaultZero()
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
                        .UseFirstOrDefault())
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
        public async Task ExecuteAsync_Should_ReturnNull_When_FirstOrDefaultZero()
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
                        .Type<ObjectType<ZeroAuthor>>()
                        .Resolve(_zeroAuthors.Take(0).AsExecutable())
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
        public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync_AsyncEnumerable()
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
                        .Resolve(new QueryableExecutable<Author>(_authors))
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
        public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault_AsyncEnumerable()
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
                        .Type<ObjectType<SingleOrDefaultAuthor>>()
                        .Resolve(
                            new QueryableExecutable<SingleOrDefaultAuthor>(_singleOrDefaultAuthors))
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
        public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultMoreThanOne_AsyncEnumerable()
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
                        .Resolve(new QueryableExecutable<Author>(_authors))
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
        public async Task ExecuteAsync_Should_ReturnNull_When_SingleOrDefaultZero_AsyncEnumerable()
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
                        .Type<ObjectType<ZeroAuthor>>()
                        .Resolve(new QueryableExecutable<ZeroAuthor>(_zeroAuthors))
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
        public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault_AsyncEnumerable()
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
                        .Resolve(new QueryableExecutable<Author>(_authors))
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
        public async Task ExecuteAsync_Should_ReturnNull_When_FirstOrDefaultZero_AsyncEnumerable()
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
                        .Type<ObjectType<ZeroAuthor>>()
                        .Resolve(new QueryableExecutable<ZeroAuthor>(_zeroAuthors))
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
    }
}
