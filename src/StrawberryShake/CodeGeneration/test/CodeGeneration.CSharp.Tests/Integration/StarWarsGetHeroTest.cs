using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using Xunit;

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
            serviceCollection.AddHttpClient(
                StarWarsGetHeroClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                StarWarsGetHeroClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddStarWarsGetHeroClient();
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
            serviceCollection.AddHttpClient(
                StarWarsGetHeroClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                StarWarsGetHeroClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddStarWarsGetHeroClient();
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
            serviceCollection.AddHttpClient(
                StarWarsGetHeroClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                StarWarsGetHeroClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddStarWarsGetHeroClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            StarWarsGetHeroClient client = services.GetRequiredService<StarWarsGetHeroClient>();

            // act
            await client.GetHero.ExecuteAsync(ct);

            string? name = null;
            IDisposable session =
                client.GetHero
                    .Watch(ExecutionStrategy.CacheFirst)
                    .Subscribe(result => name = result.Data?.Hero?.Name);

            while (name is null && !ct.IsCancellationRequested)
            {
                await Task.Delay(10, ct);
            }

            session.Dispose();

            // assert
            Assert.Equal("R2-D2", name);
        }
    }
}
