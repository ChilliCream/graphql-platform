using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Squadron;

namespace HotChocolate.Data.Raven.Test;

public class DataExtensionsTests : IClassFixture<RavenDBResource<CustomRavenDBDefaultOptions>>
{
    private readonly RavenDBResource<CustomRavenDBDefaultOptions> _resource;

    public DataExtensionsTests(RavenDBResource<CustomRavenDBDefaultOptions> resource)
    {
        _resource = resource;
    }

    [Fact]
    public async Task Pagination_Should_Work()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("""
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
    public async Task Filtering_Should_Work()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("""
            {
                filtering(where: {engine: {cylinderCount: {gte: 4}}}) {
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
    public async Task OffsetPagination_Should_Work()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("""
            {
                offset(skip: 2) {
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

    public ValueTask<IRequestExecutor> CreateExecutorAsync() => new ServiceCollection()
        .AddSingleton(CreateDocumentStore())
        .AddGraphQLServer()
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
        var documentStore = _resource.CreateDatabase($"DB{Guid.NewGuid():N}");

        using var session = documentStore.OpenSession();

        session.Store(new Car { Name = "Subaru", Engine = new Engine { CylinderCount = 6, }, });
        session.Store(new Car { Name = "Toyota", Engine = new Engine { CylinderCount = 4, }, });
        session.Store(new Car { Name = "Telsa", Engine = new Engine { CylinderCount = 0, }, });

        session.SaveChanges();

        return documentStore;
    }

    public class Query
    {
        [UsePaging(ProviderName = RavenPagination.ProviderName, IncludeTotalCount = true)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        public async Task<Connection<Car>> PagingName(
            IAsyncDocumentSession session,
            IResolverContext context,
            CancellationToken cancellationToken)
        {
            return await session.Query<Car>()
                .Filter(context)
                .Sort(context)
                .Project(context)
                .ApplyCursorPaginationAsync(context);
        }

        [UseProjection]
        [UseSorting]
        [UseFiltering]
        public async Task<List<Car>> Filtering(
            IAsyncDocumentSession session,
            IResolverContext context,
            CancellationToken cancellationToken)
        {
            return await session.Query<Car>()
                .Filter(context)
                .Sort(context)
                .Project(context)
                .ToListAsync(cancellationToken);
        }

        [UseOffsetPaging(ProviderName = RavenPagination.ProviderName, IncludeTotalCount = true)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        public async Task<CollectionSegment<Car>> Offset(
            IAsyncDocumentSession session,
            IResolverContext context,
            CancellationToken cancellationToken)
        {
            return await session.Query<Car>()
                .Filter(context)
                .Sort(context)
                .Project(context)
                .ApplyOffsetPaginationAsync(context, cancellationToken: cancellationToken);
        }
    }
}
