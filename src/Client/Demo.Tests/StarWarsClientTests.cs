using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Xunit;
using Snapshooter.Xunit;
using HotChocolate.AspNetCore;
using StrawberryShake.Client;

namespace StrawberryShake.Demo
{
    public class StarWarsClientTests
        : IClassFixture<TestServerFactory>
    {
        public StarWarsClientTests(TestServerFactory serverFactory)
        {
            ServerFactory = serverFactory;
        }

        protected TestServerFactory ServerFactory { get; set; }

        [Fact]
        public async Task GetHero_By_Episode()
        {
            // arrange
            TestServer httpServer = ServerFactory.Create(
                services => services.AddStarWars(),
                app => app.UseGraphQL());

            HttpClient httpClient = httpServer.CreateClient();
            httpClient.BaseAddress = new Uri("http://localhost:5000");

            var clientFactory = new Mock<IHttpClientFactory>();
            clientFactory.Setup(t => t.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IHttpClientFactory>(clientFactory.Object);
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
