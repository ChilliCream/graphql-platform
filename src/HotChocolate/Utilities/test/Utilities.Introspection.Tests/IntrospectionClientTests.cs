using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.Language;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.Utilities.Introspection
{
    public class IntrospectionClientTests: ServerTestBase
    {
        public IntrospectionClientTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task GetSchemaFeatures()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            HttpClient client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost:5000/graphql");

            var introspectionClient = new IntrospectionClient();

            // act
            ISchemaFeatures features = await introspectionClient.GetSchemaFeaturesAsync(client);

            // assert
            Assert.True(features.HasDirectiveLocations);
            Assert.True(features.HasRepeatableDirectives);
            Assert.True(features.HasSubscriptionSupport);
        }

        [Fact]
        public async Task GetSchemaFeatures_HttpClient_Is_Null()
        {
            // arrange
            var introspectionClient = new IntrospectionClient();

            // act
            Func<Task> action = () => introspectionClient.GetSchemaFeaturesAsync(null);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        [Fact]
        public async Task Download_Schema_AST()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            HttpClient client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost:5000/graphql");

            var introspectionClient = new IntrospectionClient();

            // act
            DocumentNode schema = await introspectionClient.DownloadSchemaAsync(client);

            // assert
            schema.ToString(true).MatchSnapshot();
        }

        [Fact]
        public async Task Download_Schema_AST_HttpClient_Is_Null()
        {
            // arrange
            var introspectionClient = new IntrospectionClient();

            // act
            Func<Task> action = () => introspectionClient.DownloadSchemaAsync(null);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        [Fact]
        public async Task Download_Schema_SDL()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            HttpClient client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost:5000/graphql");

            var introspectionClient = new IntrospectionClient();
            using var stream = new MemoryStream();

            // act
            await introspectionClient.DownloadSchemaAsync(client, stream);

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
        }

        [Fact]
        public async Task Download_Schema_SDL_HttpClient_Is_Null()
        {
            // arrange
            var introspectionClient = new IntrospectionClient();
            using var stream = new MemoryStream();

            // act
            Func<Task> action = () => introspectionClient.DownloadSchemaAsync(null, stream);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        [Fact]
        public async Task Download_Schema_SDL_Stream_Is_Null()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var introspectionClient = new IntrospectionClient();

            // act
            Func<Task> action = () =>
                introspectionClient.DownloadSchemaAsync(server.CreateClient(), null);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }
    }
}
