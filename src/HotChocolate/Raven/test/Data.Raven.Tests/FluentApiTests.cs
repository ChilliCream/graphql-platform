using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Squadron;

namespace HotChocolate.Data.Raven.Test;

public class FluentApiTests : IClassFixture<RavenDBResource<CustomRavenDBDefaultOptions>>
{
    private readonly RavenDBResource<CustomRavenDBDefaultOptions> _resource;

    public FluentApiTests(RavenDBResource<CustomRavenDBDefaultOptions> resource)
    {
        _resource = resource;
    }

    [Fact]
    public async Task Queryable_Should_BeExecuted()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("""
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
    public async Task PaginationExecutable_Should_Work()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("""
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
        var result = await executor.ExecuteAsync("""
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
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Pagination_Should_Work_When_NotAnnotedWithName()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("""
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
        var result = await executor.ExecuteAsync("""
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
        var result = await executor.ExecuteAsync("""
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
        var result = await executor.ExecuteAsync("""
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
        .AddGraphQLServer()
        .AddRavenFiltering()
        .AddRavenProjections()
        .AddRavenSorting()
        .AddRavenPagingProviders()
        .ModifyPagingOptions(x => x.RequirePagingBoundaries = false)
        .RegisterDocumentStore()
        .AddQueryType<QueryType>()
        .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
        .BuildRequestExecutorAsync();

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field("allCars")
                .Resolve(ctx => ctx.AsyncSession().Query<Car>().AsExecutable());

            descriptor.Field("pagingName")
                .Resolve(ctx => ctx.AsyncSession().Query<Car>())
                .UsePaging<ObjectType<Car>>(options: new PagingOptions
                {
                    ProviderName = RavenPagination.ProviderName, IncludeTotalCount = true,
                })
                .UseProjection()
                .UseSorting()
                .UseFiltering();

            descriptor.Field("pagingExecutable")
                .Resolve(ctx => ctx.AsyncSession().Query<Car>().AsExecutable())
                .UsePaging<ObjectType<Car>>(options: new PagingOptions
                {
                    ProviderName = RavenPagination.ProviderName, IncludeTotalCount = true,
                });

            descriptor.Field("pagingRaven")
                .Resolve(ctx => ctx.AsyncSession().Query<Car>())
                .UsePaging<ObjectType<Car>>();

            descriptor.Field("offsetPaging")
                .Resolve(ctx => ctx.AsyncSession().Query<Car>())
                .UseOffsetPaging<ObjectType<Car>>();

            descriptor.Field("firstOrDefault")
                .Resolve(ctx => ctx.AsyncSession().Query<Car>().AsExecutable())
                .UseFirstOrDefault();

            descriptor.Field("executable")
                .Resolve(ctx => ctx.AsyncSession().Query<Car>().AsExecutable())
                .UseFirstOrDefault();
        }
    }

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
}
