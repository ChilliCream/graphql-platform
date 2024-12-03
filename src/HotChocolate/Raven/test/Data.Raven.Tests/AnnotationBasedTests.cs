using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Squadron;

namespace HotChocolate.Data.Raven.Test;

public class AnnotationBasedTests(RavenDBResource<CustomRavenDBDefaultOptions> resource)
    : IClassFixture<RavenDBResource<CustomRavenDBDefaultOptions>>
{
    [Fact]
    public async Task Queryable_Should_BeExecuted()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                allCars {
                    id
                    name
                    engine {
                        cylinderCount
                    }
                }
            }
            """);

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Pagination_Should_Work()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                pagingName(first: 2) {
                    nodes {
                        id
                        name
                        engine {
                            cylinderCount
                        }
                    }
                    totalCount
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }

                }
            }
            """);

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task PaginationExecutable_Should_Work()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                pagingExecutable(first: 2) {
                    nodes {
                        id
                        name
                        engine {
                            cylinderCount
                        }
                    }
                    totalCount
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }

                }
            }
            """);

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Filtering_Should_Work()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                pagingName(where: {engine: {cylinderCount: {gte: 4}}}) {
                    nodes {
                        id
                        name
                        engine {
                            cylinderCount
                        }
                    }
                    totalCount
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }
            """);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Pagination_Should_Work_When_NotAnnotedWithName()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                pagingRaven(first: 2) {
                    nodes {
                        id
                        name
                        engine {
                            cylinderCount
                        }
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }

                }
            }
            """);

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task OffsetPaging_Should_Work()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                offsetPaging(skip:1, take:1) {
                    items {
                        id
                        name
                        engine {
                            cylinderCount
                        }
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }
            """);

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task FirstOrDefault_Should_Work()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                firstOrDefault {
                    id
                    name
                    engine {
                        cylinderCount
                    }
                }
            }
            """);

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Executable_Should_Work()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    id
                    name
                    engine {
                        cylinderCount
                    }
                }
            }
            """);

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    public ValueTask<IRequestExecutor> CreateExecutorAsync() => new ServiceCollection()
        .AddSingleton(CreateDocumentStore())
        .AddGraphQLServer(disableDefaultSecurity: true)
        .AddRavenFiltering()
        .AddRavenProjections()
        .AddRavenSorting()
        .AddRavenPagingProviders()
        .ModifyPagingOptions(o => o.RequirePagingBoundaries = false)
        .RegisterDocumentStore()
        .AddQueryType<Query>()
        .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
        .BuildRequestExecutorAsync();

    public IDocumentStore CreateDocumentStore()
    {
        var documentStore = resource.CreateDatabase($"DB{Guid.NewGuid():N}");

        using var session = documentStore.OpenSession();

        session.Store(new Car { Name = "Subaru", Engine = new Engine { CylinderCount = 6, }, });
        session.Store(new Car { Name = "Toyota", Engine = new Engine { CylinderCount = 4, }, });
        session.Store(new Car { Name = "Telsa", Engine = new Engine { CylinderCount = 0, }, });

        session.SaveChanges();

        return documentStore;
    }

    public class Query
    {
        public IQueryable<Car> AllCars(IAsyncDocumentSession session)
        {
            return session.Query<Car>();
        }

        [UsePaging(ProviderName = RavenPagination.ProviderName, IncludeTotalCount = true)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        public IQueryable<Car> PagingName(IAsyncDocumentSession session)
        {
            return session.Query<Car>();
        }

        [UsePaging(ProviderName = RavenPagination.ProviderName, IncludeTotalCount = true)]
        public IExecutable<Car> PagingExecutable(IAsyncDocumentSession session)
        {
            return session.Query<Car>().AsExecutable();
        }

        [UsePaging]
        public IRavenQueryable<Car> PagingRaven(IAsyncDocumentSession session)
        {
            return session.Query<Car>();
        }

        [UseOffsetPaging]
        public IRavenQueryable<Car> OffsetPaging(IAsyncDocumentSession session)
        {
            return session.Query<Car>();
        }

        [UseFirstOrDefault]
        public IExecutable<Car> FirstOrDefault(IAsyncDocumentSession session)
        {
            return session.Query<Car>().AsExecutable();
        }

        public IExecutable<Car> Executable(IAsyncDocumentSession session)
        {
            return session.Query<Car>().AsExecutable();
        }
    }
}
