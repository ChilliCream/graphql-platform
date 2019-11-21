using System.Threading;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using StrawberryShake.Client.GraphQL;

namespace StrawberryShake.Demo
{
    public class StarWarsClientTests
        : IntegrationTestBase
    {
        [Fact]
        public async Task GetHero_By_Episode()
        {
            // arrange
            using IWebHost host = TestServerHelper.CreateServer(out int port);
            IServiceProvider services = CreateServices(
                "StarWarsClient", port,
                s => s.AddStarWarsClient());
            IStarWarsClient client = services.GetRequiredService<IStarWarsClient>();

            // act
            IOperationResult<IGetHero> result = await client.GetHeroAsync(Episode.Empire);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task CreateReview_By_Episode()
        {
            // arrange
            using IWebHost host = TestServerHelper.CreateServer(out int port);
            IServiceProvider services = CreateServices(
                "StarWarsClient", port,
                s => s.AddStarWarsClient());
            IStarWarsClient client = services.GetRequiredService<IStarWarsClient>();

            // act
            var result = await client.CreateReviewAsync(
                Episode.Empire,
                new ReviewInput
                {
                    Commentary = "You",
                    Stars = 4
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task OnReview_By_Episode()
        {
            // arrange
            using IWebHost host = TestServerHelper.CreateServer(out int port);
            IServiceProvider services = CreateServices(
                "StarWarsClient", port,
                s => s.AddStarWarsClient());
            IStarWarsClient client = services.GetRequiredService<IStarWarsClient>();
            using var cts = new CancellationTokenSource(15000);

            // act
            var stream = await client.OnReviewAsync(Episode.Empire, cts.Token);

            // assert
            await client.CreateReviewAsync(
                Episode.Empire,
                new ReviewInput
                {
                    Commentary = "You",
                    Stars = 4
                });

            await foreach (IOperationResult<IOnReview> result in stream.WithCancellation(cts.Token))
            {
                result.MatchSnapshot();
                break;
            }

            await stream.DisposeAsync();
        }
    }
}
