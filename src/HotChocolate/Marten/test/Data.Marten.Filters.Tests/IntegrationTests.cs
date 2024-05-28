using CookieCrumble;
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
            await session.SaveChangesAsync();
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var request = OperationRequestBuilder.Create()
                .SetServices(scope.ServiceProvider)
                .SetDocument("{ foos { nodes { id } } }")
                .Build();

            var executor = await scope.ServiceProvider.GetRequestExecutorAsync();
            snapshot.Add(await executor.ExecuteAsync(request));
        }

        await snapshot.MatchMarkdownAsync();
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

    public async Task InitializeAsync() => await Container.InitializeAsync();

    public async Task DisposeAsync() => await Container.DisposeAsync();
}
