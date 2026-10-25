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

    public class Query
    {
        [UsePaging]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Foo> GetFoos(IDocumentSession session)
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
