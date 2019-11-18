
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Client.GraphQL;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.Http
{
    public class IntegrationTest
    {
        [Fact]
        public async Task Bar()
        {
            using (IWebHost host = TestServerHelper.CreateServer(out int port))
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddWebSocketClient(
                    "StarWarsClient",
                    c => c.Uri = new Uri("ws://localhost:" + port));
                serviceCollection.AddHttpClient(
                    "StarWarsClient",
                    c => c.BaseAddress = new Uri("http://localhost:" + port));
                serviceCollection.AddStarWarsClient();

                IServiceProvider services = serviceCollection.BuildServiceProvider();
                IStarWarsClient client = services.GetRequiredService<IStarWarsClient>();
                IResponseStream<IOnReview> responseStream = await client.OnReviewAsync(Episode.Empire);
                await foreach (IOperationResult<IOnReview> result in responseStream)
                {

                }
            }
        }
    }
}
