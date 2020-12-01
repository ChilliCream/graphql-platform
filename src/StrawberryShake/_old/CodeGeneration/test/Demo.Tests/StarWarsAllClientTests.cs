using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using StrawberryShake.Client.StarWarsAll;
using Xunit;

namespace StrawberryShake.Demo
{
    public class StarWarsAllClientTests
        : IntegrationTestBase
    {
        [Fact(Skip = "Fix this test")]
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

        [Fact(Skip = "Fix this test")]
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

        [Fact(Skip = "Fix this test")]
        public async Task OnReview_By_Episode()
        {
            // arrange
            using IWebHost host = TestServerHelper.CreateServer(out int port);
            IServiceProvider services = CreateServices(
                "StarWarsClient", port,
                s => s.AddStarWarsClient());
            IStarWarsClient client = services.GetRequiredService<IStarWarsClient>();
            using var cts = new CancellationTokenSource(30000);

            // act
            var stream = await client.OnReviewAsync(Episode.Empire, cts.Token);

            // assert
            await Task.Delay(5000).ConfigureAwait(false);

            await client.CreateReviewAsync(
                Episode.Empire,
                new ReviewInput
                {
                    Commentary = "You",
                    Stars = 4
                });

            await client.CreateReviewAsync(
                Episode.Empire,
                new ReviewInput
                {
                    Commentary = "Me",
                    Stars = 4
                });

            int count = 0;
            await foreach (IOperationResult<IOnReview> result in stream.WithCancellation(cts.Token))
            {
                result.MatchSnapshot(new SnapshotNameExtension(count));

                count++;
                if (count > 1)
                {
                    break;
                }
            }

            await stream.DisposeAsync();
        }
    }
}
