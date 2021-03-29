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

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsTypeNameOnInterfaces
{
    public class StarWarsTypeNameOnInterfacesTest : ServerTestBase
    {
        public StarWarsTypeNameOnInterfacesTest(TestServerFactory serverFactory) 
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Execute_StarWarsTypeNameOnInterfaces_Test()
        {
            // arrange
            using var cts = new CancellationTokenSource(20_000);

            using IWebHost host = TestServerHelper.CreateServer(
                _ => { },
                out var port);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(
                StarWarsTypeNameOnInterfacesClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                StarWarsTypeNameOnInterfacesClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddStarWarsTypeNameOnInterfacesClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            StarWarsTypeNameOnInterfacesClient client =
                services.GetRequiredService<StarWarsTypeNameOnInterfacesClient>();

            // act
            var result = await client.GetHero.ExecuteAsync();

            // assert
            result.EnsureNoErrors();
            result.Data.MatchSnapshot();
        }
    }
}
