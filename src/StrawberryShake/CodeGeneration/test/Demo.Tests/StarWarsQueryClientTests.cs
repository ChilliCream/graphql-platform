using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Client.StarWarsQuery;
using Xunit;

namespace StrawberryShake.Demo
{
    public class StarWarsQueryClientTests
        : IntegrationTestBase
    {
        [Fact(Skip = "Fix this test")]
        public async Task GetHuman_By_Id()
        {
            // arrange
            using IWebHost host = TestServerHelper.CreateServer(out int port);
            IServiceProvider services = CreateServices(
                "StarWarsClient", port,
                s => s.AddStarWarsClient());
            IStarWarsClient client = services.GetRequiredService<IStarWarsClient>();

            // act
            IOperationResult<IGetHuman> result = await client.GetHumanAsync("1001");

            // assert
            result.MatchSnapshot();
        }
    }
}
