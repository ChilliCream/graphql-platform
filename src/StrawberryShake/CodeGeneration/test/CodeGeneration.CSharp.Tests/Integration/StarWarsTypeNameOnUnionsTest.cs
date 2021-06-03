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

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsTypeNameOnUnions
{
    public class StarWarsTypeNameOnUnionsTest : ServerTestBase
    {
        public StarWarsTypeNameOnUnionsTest(TestServerFactory serverFactory) : base(serverFactory)
        {
        }

        [Fact]
        public async Task Execute_StarWarsTypeNameOnUnions_Test()
        {
            // arrange
            var cts = new CancellationTokenSource(20_000);
            using IWebHost host = TestServerHelper.CreateServer(
                _ => { },
                out var port);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(
                StarWarsTypeNameOnUnionsClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                StarWarsTypeNameOnUnionsClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddStarWarsTypeNameOnUnionsClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            StarWarsTypeNameOnUnionsClient client = 
                services.GetRequiredService<StarWarsTypeNameOnUnionsClient>();

           // act
            var result = await client.SearchHero.ExecuteAsync(cts.Token);

            // assert
            result.EnsureNoErrors();
            result.Data.MatchSnapshot();
        }
    }
}
