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

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsIntrospection
{
    public class StarWarsIntrospectionTest : ServerTestBase
    {
        public StarWarsIntrospectionTest(TestServerFactory serverFactory) : base(serverFactory)
        {
        }

        [Fact]
        public async Task Execute_StarWarsIntrospection_Test()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            using IWebHost host = TestServerHelper.CreateServer(
                _ => { },
                out var port);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(
                StarWarsIntrospectionClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                StarWarsIntrospectionClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddStarWarsIntrospectionClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            StarWarsIntrospectionClient client = services.GetRequiredService<StarWarsIntrospectionClient>();

            // act
            IOperationResult<IIntrospectionQueryResult> result =
                await client.IntrospectionQuery.ExecuteAsync(ct);


            // assert
            result.MatchSnapshot();
        }
    }
}
