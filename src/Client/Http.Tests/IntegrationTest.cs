
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
                serviceCollection.AddHttpClient(
                    "StarWarsClient",
                    c => c.BaseAddress = new Uri("http://localhost:" + port));
                serviceCollection.AddWebSocketClient(
                    "StarWarsClient",
                    c => c.Uri = new Uri("ws://localhost:" + port));
                serviceCollection.AddStarWarsClient();

                IServiceProvider services = serviceCollection.BuildServiceProvider();
                IStarWarsClient client = services.GetRequiredService<IStarWarsClient>();

                var stream = await client.OnReviewAsync(Episode.Empire);

                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    await client.CreateReviewAsync(Episode.Empire, new ReviewInput { Commentary = "jfkdjfk", Stars = 1 });
                });

                await foreach (IOperationResult<IOnReview> result in stream)
                {
                    if (result.Data is { })
                    {
                        Console.WriteLine(result.Data.OnReview.Commentary);
                    }
                }
            }
        }
    }
}
