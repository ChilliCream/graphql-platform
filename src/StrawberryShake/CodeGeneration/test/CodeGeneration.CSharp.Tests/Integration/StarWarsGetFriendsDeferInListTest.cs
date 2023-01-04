using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetFriendsDeferInList
{
    public class StarWarsGetFriendsDeferInListTest : ServerTestBase
    {
        public StarWarsGetFriendsDeferInListTest(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Execute_StarWarsGetFriendsDeferInList_Test()
        {
            // arrange
            var ct = new CancellationTokenSource(20_000).Token;
            using var host = TestServerHelper.CreateServer(_ => { }, out var port);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(
                StarWarsGetFriendsDeferInListClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                StarWarsGetFriendsDeferInListClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddStarWarsGetFriendsDeferInListClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            var client = services.GetRequiredService<StarWarsGetFriendsDeferInListClient>();

            // act
            var result = await client.GetHero.ExecuteAsync(ct);

            // assert
            Assert.NotNull(result.Data?.Hero?.Friends?.Nodes?[0]?.CharacterName);
        }
    }
}
