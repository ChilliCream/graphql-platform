using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Xunit;
using HotChocolate.AspNetCore;
using StrawberryShake.Client.GraphQL;

namespace StrawberryShake.Demo
{
    public class StarWarsClientTests
        : IClassFixture<TestServerFactory>
    {
        public StarWarsClientTests(TestServerFactory serverFactory)
        {
            ServerFactory = serverFactory;

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

            Services = serviceCollection.BuildServiceProvider();

        }

        protected TestServerFactory ServerFactory { get; set; }

        protected ServiceProvider Services { get; set; }

        [Fact]
        public async Task GetHero_By_Episode()
        {
            // arrange
            IStarWarsClient client = Services.GetRequiredService<IStarWarsClient>();

            // act
            IOperationResult<IGetHero> result = await client.GetHeroAsync(Episode.Empire);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task CreateReview_By_Episode()
        {
            // arrange
            IStarWarsClient client = Services.GetRequiredService<IStarWarsClient>();
            IValueSerializerResolver serializerResolver = Services.GetRequiredService<IValueSerializerResolver>();

            // act
            var result = await client.CreateReviewAsync(Episode.Empire, new ReviewInput() { Commentary = "You", Stars = 4 });

            // assert
            result.MatchSnapshot();
        }
    }
}
