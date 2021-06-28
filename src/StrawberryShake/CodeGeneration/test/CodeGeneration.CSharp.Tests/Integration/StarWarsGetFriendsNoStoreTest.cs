using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetFriendsNoStore
{
    public class StarWarsGetFriendsNoStoreTest : ServerTestBase
    {
        public StarWarsGetFriendsNoStoreTest(TestServerFactory serverFactory) : base(serverFactory)
        {
        }

        [Fact]
        public async Task Execute_StarWarsGetFriendsNoStore_Test()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            using IWebHost host = TestServerHelper.CreateServer(
                _ => { },
                out var port);
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddStarWarsGetFriendsNoStoreClient()
                .ConfigureHttpClient(
                    c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
                .ConfigureWebSocketClient(
                    c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            IStarWarsGetFriendsNoStoreClient client =
                services.GetRequiredService<IStarWarsGetFriendsNoStoreClient>();

            // act
            IOperationResult<IGetHeroResult> result = await client.GetHero.ExecuteAsync(ct);

            // assert
            Assert.Equal("R2-D2", result.Data?.Hero?.Name);
            Assert.Collection(
                result.Data!.Hero!.Friends!.Nodes!,
                item => Assert.Equal("Luke Skywalker", item?.Name),
                item => Assert.Equal("Han Solo", item?.Name),
                item => Assert.Equal("Leia Organa", item?.Name));
        }
    }
}
