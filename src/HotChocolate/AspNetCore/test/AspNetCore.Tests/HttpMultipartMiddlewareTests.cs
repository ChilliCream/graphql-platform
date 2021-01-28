using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.TestHost;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class HttpMultipartMiddlewareTests : ServerTestBase
    {
        public HttpMultipartMiddlewareTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task EmptyForm_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent();

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task EmptyOperations_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(""), "operations" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task IncompleteOperations_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{}"), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task MapWithNoOperations_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task MapBeforeOperations_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "map" },
                { new StringContent("{}"), "operations" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task EmptyMap_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent(""), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task InvalidMap_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("data"), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task MissingFile_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            var request = new ClientQueryRequest
            {
                // TODO : needs a valid query to execute
                Query = "",
                Variables = new Dictionary<string, object>
                {
                    { "1", new[] { "variables.file" } }
                }
            };

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(JsonSerializer.Serialize(request)), "operations" },
                { new StringContent("{ \"1\": [\"variables.file\"] }"), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExtraneousFile_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            var request = new ClientQueryRequest
            {
                // TODO : needs a valid query to execute
                Query = "",
                Variables = new Dictionary<string, object>
                {
                    { "1", new[] { "variables.file" } }
                }
            };

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(JsonSerializer.Serialize(request)), "operations" },
                { new StringContent("{\"1\": [\"variables.file\"]}"), "map" },
                { new StringContent("File1"), "1", "file1.txt" },
                { new StringContent("File2"), "2", "file2.txt" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }
    }
}