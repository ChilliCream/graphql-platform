using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetFriendsDeferred
{
    public class StarWarsGetFriendsDeferredTest : ServerTestBase
    {
        public StarWarsGetFriendsDeferredTest(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Execute_StarWarsGetFriendsDeferred_Test()
        {
            // arrange
            var ct = new CancellationTokenSource(20_000).Token;
            using var host = TestServerHelper.CreateServer(_ => { }, out var port);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(
                StarWarsGetFriendsDeferredClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                StarWarsGetFriendsDeferredClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddStarWarsGetFriendsDeferredClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            var client = services.GetRequiredService<StarWarsGetFriendsDeferredClient>();

            // act
            var result = await client.GetHero.ExecuteAsync(ct);

            // assert
            Assert.NotNull(result.Data?.Hero?.FriendsListLabel);
            Assert.NotNull(result.Data?.Hero?.FriendsListLabel?.Friends?.Nodes);
            Assert.Equal(3, result.Data?.Hero?.FriendsListLabel?.Friends?.Nodes?.Count);
        }

        [Fact]
        public void Watch_StarWarsGetFriendsDeferred_Test()
        {
            // arrange
            using var host = TestServerHelper.CreateServer(_ => { }, out var port);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(
                StarWarsGetFriendsDeferredClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                StarWarsGetFriendsDeferredClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddStarWarsGetFriendsDeferredClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            var client = services.GetRequiredService<StarWarsGetFriendsDeferredClient>();
            var waitHandle = new AutoResetEvent(false);
            var updates = new List<bool>();

            // act
            using var session = client.GetHero.Watch().Subscribe(result =>
            {
                updates.Add(result.Data?.Hero?.FriendsListLabel is not null);

                if (updates.Count is 2)
                {
                    waitHandle.Set();
                }
            });

            waitHandle.WaitOne(1000);

            // assert
            Assert.Collection(updates, Assert.False, Assert.True);
        }
    }
}
