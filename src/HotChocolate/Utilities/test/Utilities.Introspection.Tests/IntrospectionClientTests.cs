using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Utilities.Introspection
{
    public class IntrospectionClientTests
        : ServerTestBase
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
            var introspectionClient = new IntrospectionClient();

            // act
            ISchemaFeatures features =
                await introspectionClient.GetSchemaFeaturesAsync(
                        server.CreateClient());

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
            var introspectionClient = new IntrospectionClient();

            // act
            DocumentNode schema =
                await introspectionClient.DownloadSchemaAsync(
                        server.CreateClient());

            // assert
            SchemaSyntaxSerializer.Serialize(schema, true).MatchSnapshot();
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
            var introspectionClient = new IntrospectionClient();
            using var stream = new MemoryStream();

            // act
            await introspectionClient.DownloadSchemaAsync(
                server.CreateClient(), stream);

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
