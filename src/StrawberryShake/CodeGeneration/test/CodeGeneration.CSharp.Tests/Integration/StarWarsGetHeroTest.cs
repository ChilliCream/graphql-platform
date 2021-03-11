using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using LiteDB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.State;
using StrawberryShake.Transport.WebSockets;
using StrawberryShake.Extensions;
using Xunit;
using Xunit.Sdk;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    public class StarWarsGetHeroTest : ServerTestBase
    {
        public StarWarsGetHeroTest(TestServerFactory serverFactory) : base(serverFactory)
        {
        }

        [Fact]
        public async Task Execute_StarWarsGetHero_Test()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            using IWebHost host = TestServerHelper.CreateServer(
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
            StarWarsGetHeroClient client = services.GetRequiredService<StarWarsGetHeroClient>();

            // act
            IOperationResult<IGetHeroResult> result = await client.GetHero.ExecuteAsync(ct);

            // assert
            Assert.Equal("R2-D2", result.Data!.Hero!.Name);
        }

        [Fact]
        public async Task Watch_StarWarsGetHero_Test()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            using IWebHost host = TestServerHelper.CreateServer(
                _ => { },
                out var port);
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddStarWarsGetHeroClient()
                .ConfigureHttpClient(
                    c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
                .ConfigureWebSocketClient(
                    c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            StarWarsGetHeroClient client = services.GetRequiredService<StarWarsGetHeroClient>();

            // act
            string? name = null;
            IDisposable session =
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
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            using IWebHost host = TestServerHelper.CreateServer(
                _ => { },
                out var port);
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddStarWarsGetHeroClient()
                .ConfigureHttpClient(
                    c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
                .ConfigureWebSocketClient(
                    c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            StarWarsGetHeroClient client = services.GetRequiredService<StarWarsGetHeroClient>();

            // act
            await client.GetHero.ExecuteAsync(ct);

            string? name = null;
            IDisposable session =
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
            using IWebHost host = TestServerHelper.CreateServer(
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
            IDisposable session =
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
            string fileName = Path.GetTempFileName();
            File.Delete(fileName);

            try
            {
                {
                    // arrange
                    using var cts = new CancellationTokenSource(100_20_000);
                    using IWebHost host = TestServerHelper.CreateServer(
                        _ => { },
                        out var port);
                    var serviceCollection = new ServiceCollection();

                    serviceCollection
                        .AddStarWarsGetHeroClient()
                        .ConfigureHttpClient(
                            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
                        .ConfigureWebSocketClient(
                            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"))
                        .AddLiteDBPersistence(fileName);

                    using var services = serviceCollection.BuildServiceProvider();
                    services.GetRequiredService<LiteDBPersistence>().Initialize();
                    var client = services.GetRequiredService<StarWarsGetHeroClient>();
                    var storeAccessor =
                        services.GetRequiredService<StarWarsGetHeroClientStoreAccessor>();
                    var entityId = new EntityId("Droid", "2001");
                    await Task.Delay(250);

                    // act
                    await client.GetHero.ExecuteAsync(cts.Token);

                    string? name = null;
                    IDisposable session =
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

                    await Task.Delay(500);
                }
                {
                    // arrange
                    using var cts = new CancellationTokenSource(100_20_000);
                    using IWebHost host = TestServerHelper.CreateServer(
                        _ => { },
                        out var port);
                    var serviceCollection = new ServiceCollection();

                    serviceCollection
                        .AddStarWarsGetHeroClient()
                        .ConfigureHttpClient(
                            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
                        .ConfigureWebSocketClient(
                            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"))
                        .AddLiteDBPersistence(fileName);

                    using var services = serviceCollection.BuildServiceProvider();
                    services.GetRequiredService<LiteDBPersistence>().Initialize();
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
                    File.Delete(fileName);
                }
            }
        }
    }
}
