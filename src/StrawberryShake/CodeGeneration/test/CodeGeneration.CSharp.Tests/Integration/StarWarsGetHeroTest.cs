using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.State;
using StrawberryShake.Transport.WebSockets;
using StrawberryShake.Persistence.SQLite;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero;

public class StarWarsGetHeroTest : ServerTestBase
{
    public StarWarsGetHeroTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_StarWarsGetHero_Test()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddStarWarsGetHeroClient()
            .ConfigureHttpClient(
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
            .ConfigureWebSocketClient(
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<StarWarsGetHeroClient>();

        // act
        var result = await client.GetHero.ExecuteAsync(ct);

        // assert
        Assert.Equal("R2-D2", result.Data!.Hero!.Name);
    }

    [Fact]
    public async Task Watch_StarWarsGetHero_Test()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddStarWarsGetHeroClient()
            .ConfigureHttpClient(
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
            .ConfigureWebSocketClient(
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<StarWarsGetHeroClient>();

        // act
        string? name = null;
        var session =
            client.GetHero
                .Watch()
                .Subscribe(result => name = result.Data?.Hero?.Name);

        while (name is null && !ct.IsCancellationRequested)
        {
            await Task.Delay(10, ct);
        }

        session.Dispose();

        // assert
        Assert.Equal("R2-D2", name);
    }

    [Fact]
    public async Task Watch_CacheFirst_StarWarsGetHero_Test()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddStarWarsGetHeroClient()
            .ConfigureHttpClient(
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
            .ConfigureWebSocketClient(
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<StarWarsGetHeroClient>();

        // act
        await client.GetHero.ExecuteAsync(ct);

        string? name = null;
        var session =
            client.GetHero
                .Watch(ExecutionStrategy.CacheFirst)
                .Subscribe(result =>
                {
                    name = result.Data?.Hero?.Name;
                });

        while (name is null && !ct.IsCancellationRequested)
        {
            await Task.Delay(10, ct);
        }

        session.Dispose();

        // assert
        Assert.Equal("R2-D2", name);
    }

    [Fact]
    public async Task Watch_Interact_With_Store()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddStarWarsGetHeroClient()
            .ConfigureHttpClient(
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
            .ConfigureWebSocketClient(
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        var services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<StarWarsGetHeroClient>();
        var storeAccessor = services.GetRequiredService<StarWarsGetHeroClientStoreAccessor>();
        var entityId = new EntityId("Droid", "2001");

        // act
        await client.GetHero.ExecuteAsync(cts.Token);

        string? name = null;
        var session =
            client.GetHero
                .Watch(ExecutionStrategy.CacheFirst)
                .Subscribe(result => name = result.Data?.Hero?.Name);

        while (name is null && !cts.Token.IsCancellationRequested)
        {
            await Task.Delay(10, cts.Token);
        }

        var name1 = name;
        name = null;

        storeAccessor.EntityStore.Update(s =>
        {
            if (s.CurrentSnapshot.TryGetEntity(entityId, out DroidEntity? entity))
            {
                entity = new DroidEntity("NewName");
                s.SetEntity(entityId, entity);
            }
        });

        while (name is null && !cts.Token.IsCancellationRequested)
        {
            await Task.Delay(10, cts.Token);
        }

        var name2 = name;
        name = null;

        session.Dispose();

        // assert
        Assert.Equal("R2-D2", name1);
        Assert.Equal("NewName", name2);
    }

    [Fact]
    public async Task Watch_Interact_With_Persistence()
    {
        var fileName = Path.GetTempFileName();
        var connectionString = "Data Source=" + fileName;
        File.Delete(fileName);

        try
        {
            {
                // arrange
                using var cts = new CancellationTokenSource(100_20_000);
                using var host = TestServerHelper.CreateServer(
                    _ => { },
                    out var port);
                var serviceCollection = new ServiceCollection();

                serviceCollection
                    .AddStarWarsGetHeroClient()
                    .ConfigureHttpClient(
                        c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
                    .ConfigureWebSocketClient(
                        c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"))
                    .AddSQLitePersistence(connectionString);

                await using var services = serviceCollection.BuildServiceProvider();
                await services.GetRequiredService<SQLitePersistence>().InitializeAsync();
                var client = services.GetRequiredService<StarWarsGetHeroClient>();
                var storeAccessor =
                    services.GetRequiredService<StarWarsGetHeroClientStoreAccessor>();
                var entityId = new EntityId("Droid", "2001");
                await Task.Delay(250, cts.Token);

                // act
                await client.GetHero.ExecuteAsync(cts.Token);

                string? name = null;
                var session =
                    client.GetHero
                        .Watch(ExecutionStrategy.CacheFirst)
                        .Subscribe(result => name = result.Data?.Hero?.Name);

                while (name is null && !cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(10, cts.Token);
                }

                var name1 = name;
                name = null;

                storeAccessor.EntityStore.Update(s =>
                {
                    if (s.CurrentSnapshot.TryGetEntity(entityId, out DroidEntity? entity))
                    {
                        entity = new DroidEntity("NewName");
                        s.SetEntity(entityId, entity);
                    }
                });

                while (name is null && !cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(10, cts.Token);
                }

                var name2 = name;
                name = null;

                session.Dispose();

                // assert
                Assert.Equal("R2-D2", name1);
                Assert.Equal("NewName", name2);

                await Task.Delay(500, cts.Token);
            }
            {
                // arrange
                using var cts = new CancellationTokenSource(100_20_000);
                using var host = TestServerHelper.CreateServer(
                    _ => { },
                    out var port);
                var serviceCollection = new ServiceCollection();

                serviceCollection
                    .AddStarWarsGetHeroClient()
                    .ConfigureHttpClient(
                        c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
                    .ConfigureWebSocketClient(
                        c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"))
                    .AddSQLitePersistence(connectionString);

                await using var services = serviceCollection.BuildServiceProvider();
                await services.GetRequiredService<SQLitePersistence>().InitializeAsync();
                var client = services.GetRequiredService<StarWarsGetHeroClient>();

                // act
                string? name = null;
                client.GetHero
                    .Watch(ExecutionStrategy.CacheFirst)
                    .Subscribe(result =>
                    {
                        name = result.Data!.Hero!.Name;
                    });

                while (name is null && !cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(10, cts.Token);
                }

                // assert
                Assert.Equal("NewName", name);
            }
        }
        finally
        {
            if (File.Exists(fileName))
            {
                SqliteConnection.ClearAllPools();
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public async Task Update_Once()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddStarWarsGetHeroClient()
            .ConfigureHttpClient(
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
            .ConfigureWebSocketClient(
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<StarWarsGetHeroClient>();

        await client.GetHero.ExecuteAsync(ct);

        // act
        var count = 0;
        using var session =
            client.GetHero
                .Watch(ExecutionStrategy.CacheAndNetwork)
                .Subscribe(_ =>
                {
                    count++;
                });

        await Task.Delay(1000, ct);

        // assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Update_Once_With_Cache_And_Network()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddStarWarsGetHeroClient()
            .ConfigureHttpClient(
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
            .ConfigureWebSocketClient(
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<StarWarsGetHeroClient>();

        // act
        var count = 0;
        using var session =
            client.GetHero
                .Watch(ExecutionStrategy.CacheAndNetwork)
                .Subscribe(_ =>
                {
                    count++;
                });

        await Task.Delay(1000, ct);

        // assert
        Assert.Equal(1, count);
    }
}
