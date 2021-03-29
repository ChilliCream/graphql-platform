using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsUnionList
{
    public class StarWarsUnionListTest : ServerTestBase
    {
        public StarWarsUnionListTest(TestServerFactory serverFactory) : base(serverFactory)
        {
        }

        [Fact]
        public async Task Execute_StarWarsUnionList_Test()
        {
            // arrange
            using var cts = new CancellationTokenSource(20_000);
            
            using IWebHost host = TestServerHelper.CreateServer(
                _ => { },
                out var port);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(
                StarWarsUnionListClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                StarWarsUnionListClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddStarWarsUnionListClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            StarWarsUnionListClient client = services.GetRequiredService<StarWarsUnionListClient>();

            // act
            var result = await client.SearchHero.ExecuteAsync(cts.Token);

            // assert
            result.EnsureNoErrors();
            result.Data.MatchSnapshot();
        }
    }
}
