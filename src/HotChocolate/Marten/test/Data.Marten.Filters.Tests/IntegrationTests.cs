using HotChocolate.Execution;
using HotChocolate.Types;
using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class IntegrationTests : IAsyncLifetime
{
    protected static ResourceContainer Container { get; } = new();

    [Fact]
    public async Task Ensure_Marten_Works()
    {
        var snapshot = new Snapshot();
        var dbName = $"DB_{Guid.NewGuid():N}";
        await Container.Resource.CreateDatabaseAsync(dbName);

        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddMarten(Container.Resource.GetConnectionString(dbName));

        serviceCollection
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddMartenFiltering()
            .AddMartenSorting();

        var services = serviceCollection.BuildServiceProvider();

        await using (var scope = services.CreateAsyncScope())
        {
            await using var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
            session.Store(new Foo { Id = 1, Bar = true });
            await session.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var request = OperationRequestBuilder.New()
                .SetServices(scope.ServiceProvider)
                .SetDocument("{ foos { nodes { id } } }")
                .Build();

            var executor = await scope.ServiceProvider.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);
            snapshot.Add(await executor.ExecuteAsync(request, TestContext.Current.CancellationToken));
        }

        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("{ filteredFoos { id } }", "{\"data\":{\"filteredFoos\":[{\"id\":1}]}}")]
    [InlineData("{ sortedFoos { id } }", "{\"data\":{\"sortedFoos\":[{\"id\":1}]}}")]
    [InlineData("{ executableFoos { id } }", "{\"data\":{\"executableFoos\":[{\"id\":1}]}}")]
    [InlineData("{ martenFoos { id } }", "{\"data\":{\"martenFoos\":[{\"id\":1}]}}")]
    [InlineData("{ firstFoo { id } }", "{\"data\":{\"firstFoo\":{\"id\":1}}}")]
    [InlineData("{ singleFoo { id } }", "{\"data\":{\"singleFoo\":{\"id\":1}}}")]
    [InlineData("{ foos { nodes { id } totalCount } }", "{\"data\":{\"foos\":{\"nodes\":[{\"id\":1}],\"totalCount\":1}}}")]
    public async Task Execute_Should_UseAsyncMartenOperations_When_ResolverReturnsQueryable(
        string document,
        string expected)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var dbName = $"DB_{Guid.NewGuid():N}";
        await Container.Resource.CreateDatabaseAsync(dbName);

        var services = new ServiceCollection();
        services.AddMarten(Container.Resource.GetConnectionString(dbName));
        services
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddMartenFiltering()
            .AddMartenSorting()
            .ModifyPagingOptions(options => options.IncludeTotalCount = true);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        session.Store(new Foo { Id = 1, Bar = true });
        await session.SaveChangesAsync(cancellationToken);

        var executor = await scope.ServiceProvider.GetRequestExecutorAsync(cancellationToken: cancellationToken);
        var request = OperationRequestBuilder.New()
            .SetServices(scope.ServiceProvider)
            .SetDocument(document)
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, cancellationToken);

        // assert
        Assert.Equal(expected, result.ToJson(withIndentations: false));
    }

    public class Query
    {
        [UsePaging]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Foo> GetFoos(IDocumentSession session)
            => session.Query<Foo>();

        [UseFiltering]
        public IQueryable<Foo> GetFilteredFoos(IDocumentSession session)
            => session.Query<Foo>();

        [UseSorting]
        public IQueryable<Foo> GetSortedFoos(IDocumentSession session)
            => session.Query<Foo>();

        [UseFiltering]
        public IQueryableExecutable<Foo> GetExecutableFoos(IDocumentSession session)
            => session.Query<Foo>().AsExecutable();

        public IQueryableExecutable<Foo> GetMartenFoos(IDocumentSession session)
            => session.Query<Foo>().AsMartenExecutable();

        [UseFirstOrDefault]
        [UseFiltering]
        public IQueryable<Foo> GetFirstFoo(IDocumentSession session)
            => session.Query<Foo>();

        [UseSingleOrDefault]
        [UseSorting]
        public IQueryable<Foo> GetSingleFoo(IDocumentSession session)
            => session.Query<Foo>();
    }

    public class Foo
    {
        public int Id { get; set; }

        public bool Bar { get; set; }
    }

    public async ValueTask InitializeAsync() => await Container.InitializeAsync();

    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
