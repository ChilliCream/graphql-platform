using System.Net;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using StrawberryShake;
using StrawberryShake.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StrawberryShake.Http.Pipelines;
using Xunit;
using StrawberryShake.Serializers;
using StrawberryShake.Client;
using Snapshooter.Xunit;

namespace Demo.Tests
{
    public class StarWarsClientTests
    {
        [Fact]
        public async Task GetHero_By_Episode()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddHttpClient("StarWarsClient")
                .ConfigureHttpClient(t => t.BaseAddress = new Uri("http://localhost:5000/graphql"));
            serviceCollection.AddDefaultScalarSerializers();
            serviceCollection.AddStarWarsClient();

            var services = serviceCollection.BuildServiceProvider();

            // act
            IStarWarsClient client = services.GetRequiredService<IStarWarsClient>();
            IOperationResult<IGetHero> result = await client.GetHeroAsync(Episode.Empire);

            // assert
            result.MatchSnapshot();
        }


    }
}
