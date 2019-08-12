using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Language;
using Microsoft.AspNetCore.TestHost;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class PostMiddlewareTests
        : ServerTestBase
    {
        public PostMiddlewareTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task HttpPost_Json_QueryOnly()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    {
                        hero {
                            name
                        }
                    }
                "
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Check_Response_ContentType()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    {
                        hero {
                            name
                        }
                    }
                "
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            Assert.Collection(
                message.Content.Headers.GetValues("Content-Type"),
                t => Assert.Equal("application/json", t));
        }

        [Fact]
        public async Task HttpPost_Json_QueryAndEnumVariable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    query h($episode: Episode) {
                        hero(episode: $episode) {
                            name
                        }
                    }
                ",
                Variables = new Dictionary<string, object>
                {
                    { "episode", "EMPIRE" }
                }
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Json_QueryAndStringVariable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    query h($id: String!) {
                        human(id: $id) {
                            name
                        }
                    }
                ",
                Variables = new Dictionary<string, object>
                {
                    { "id", "1000" }
                }
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Json_OnRoot()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    {
                        hero {
                            name
                        }
                    }
                "
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Json_OnSubPath()
        {
            // arrange
            TestServer server = CreateStarWarsServer("/foo");
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    {
                        hero {
                            name
                        }
                    }
                "
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, "foo");

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Json_OnSubPath_PostOnInvalidPath()
        {
            // arrange
            TestServer server = CreateStarWarsServer("/foo");
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    {
                        hero {
                            name
                        }
                    }
                "
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, "bar");

            // assert
            Assert.Equal(HttpStatusCode.NotFound, message.StatusCode);
        }

        [Fact]
        public async Task HttpPost_Ensure_Response_Casing_Alignes_With_Request()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    {
                        Hero: hero {
                            name
                        }
                    }
                "
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Json_Object_Variable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $review: ReviewInput!) {
                        createReview(episode: $ep, review: $review) {
                            stars
                            commentary
                        }
                    }
                ",
                Variables = new Dictionary<string, object>
                {
                    { "ep", "EMPIRE" },
                    { "review",
                        new Dictionary<string, object>
                        {
                            { "stars", 5 },
                            { "commentary", "This is a great movie!" },
                        }
                    }
                }
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Json_Variable_NonNull_Violation()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $review: ReviewInput!) {
                        createReview(episode: $ep, review: $review) {
                            stars
                            commentary
                        }
                    }
                ",
                Variables = new Dictionary<string, object>
                {
                    { "review",
                        new Dictionary<string, object>
                        {
                            { "stars", 5 },
                            { "commentary", "This is a great movie!" },
                        }
                    }
                }
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Json_Variables_In_Object_Fields()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $stars: Int!
                        $commentary: String!) {
                        createReview(episode: $ep, review: {
                            stars: $stars
                            commentary: $commentary
                        } ) {
                            stars
                            commentary
                        }
                    }
                ",
                Variables = new Dictionary<string, object>
                {
                    { "ep", "EMPIRE" },
                    { "stars", 5 },
                    { "commentary", "This is a great movie!" }
                }
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Json_Unused_Variable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $stars: Int!
                        $commentary: String!
                        $foo: Float) {
                        createReview(episode: $ep, review: {
                            stars: $stars
                            commentary: $commentary
                        } ) {
                            stars
                            commentary
                        }
                    }
                ",
                Variables = new Dictionary<string, object>
                {
                    { "ep", "EMPIRE" },
                    { "stars", 5 },
                    { "commentary", "This is a great movie!" }
                }
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [InlineData("a")]
        [InlineData("b")]
        [Theory]
        public async Task HttpPost_Json_QueryAndOperationName(
            string operationName)
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    query a {
                        a: hero {
                            name
                        }
                    }

                    query b {
                        b: hero {
                            name
                        }
                    }
                ",
                OperationName = operationName
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot(new SnapshotNameExtension(operationName));
        }

        [Fact]
        public async Task HttpPost_Json_CachedQuery()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    query a {
                        hero {
                            name
                        }
                    }
                ".Replace("\n", string.Empty).Replace("\r", string.Empty),
            };

            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // act
            request = new ClientQueryRequest
            {
                NamedQuery = "W5vrrAIypCbniaIYeroNnw=="
            };

            message = await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Json_CachedQuery_2()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    query a {
                        hero {
                            name
                        }
                    }
                ".Replace("\n", string.Empty).Replace("\r", string.Empty),
                NamedQuery = "W5vrrAIypCbniaIYeroNnw=="
            };

            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // act
            request = new ClientQueryRequest
            {
                NamedQuery = "W5vrrAIypCbniaIYeroNnw=="
            };

            message = await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Json_CachedQuery_NotFound()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    query a {
                        hero {
                            name
                        }
                    }
                ".Replace("\n", string.Empty).Replace("\r", string.Empty),
                NamedQuery = "abc"
            };

            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // act
            request = new ClientQueryRequest
            {
                NamedQuery = "abc"
            };

            message = await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Plain_GraphQL()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request =
                @"
                    {
                        hero {
                            name
                        }
                    }
                ";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_UnknownContentType()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request =
                @"
                    {
                        hero {
                            name
                        }
                    }
                ";
            var contentType = "application/foo";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.BadRequest, message.StatusCode);
        }

        [InlineData("/", null, HttpStatusCode.OK)]
        [InlineData("/", "/", HttpStatusCode.OK)]
        [InlineData("/graphql", "/graphql/", HttpStatusCode.OK)]
        [InlineData("/graphql", "/graphql", HttpStatusCode.OK)]
        [InlineData("/graphql/", "/graphql", HttpStatusCode.OK)]
        [InlineData("/graphql/", "/graphql/", HttpStatusCode.OK)]
        [InlineData("/graphql", "/graphql/foo", HttpStatusCode.NotFound)]
        [InlineData("/graphql/foo", "/graphql/foo/", HttpStatusCode.OK)]
        [InlineData("/graphql/foo", "/graphql/foo", HttpStatusCode.OK)]
        [Theory]
        public async Task HttpPost_Path(
            string path,
            string requestPath,
            HttpStatusCode httpStatus)
        {
            // arrange
            TestServer server = CreateStarWarsServer(path);
            var request = new ClientQueryRequest
            {
                Query =
                @"
                    query a {
                        hero {
                            name
                        }
                    }
                "
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, requestPath);

            // assert
            Assert.Equal(httpStatus, message.StatusCode);

            if (message.StatusCode == HttpStatusCode.OK)
            {
                ClientQueryResult result = await DeserializeAsync(message);
                result.MatchSnapshot(new SnapshotNameExtension(
                    path?.Replace("/", "_Slash_"),
                    requestPath?.Replace("/", "_Slash_")));
            }
        }

        [Fact]
        public async Task HttpPost_Batch()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var batch = new List<ClientQueryRequest>
            {
                new ClientQueryRequest
                {
                    Query =
                    @"
                    query getHero {
                        hero(episode: EMPIRE) {
                            id @export
                        }
                    }"
                },
                new ClientQueryRequest
                {
                    Query =
                    @"
                    query getHuman {
                        human(id: $id) {
                            name
                        }
                    }"
                }
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(batch);

            // assert
            List<ClientQueryResult> result =
                 await DeserializeBatchAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpPost_Batch_ContentType()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var batch = new List<ClientQueryRequest>
            {
                new ClientQueryRequest
                {
                    Query =
                    @"
                    query getHero {
                        hero(episode: EMPIRE) {
                            id @export
                        }
                    }"
                },
                new ClientQueryRequest
                {
                    Query =
                    @"
                    query getHuman {
                        human(id: $id) {
                            name
                        }
                    }"
                }
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(batch);

            // assert
            Assert.Collection(
                message.Content.Headers.GetValues("Content-Type"),
                    t => Assert.Equal("application/json", t));
        }

        [Fact]
        public async Task HttpPost_Operation_Batch()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var batch = new List<ClientQueryRequest>
            {
                new ClientQueryRequest
                {
                    Query =
                    @"
                    query getHero {
                        hero(episode: EMPIRE) {
                            id @export
                        }
                    }

                    query getHuman {
                        human(id: $id) {
                            name
                        }
                    }"
                }
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(
                    batch,
                    "?batchOperations=[getHero, getHuman]");

            // assert
            List<ClientQueryResult> result =
                 await DeserializeBatchAsync(message);
            result.MatchSnapshot();
        }


        [InlineData("?batchOperations=getHero")]
        [InlineData("?batchOperations=[getHero")]
        [Theory]
        public async Task HttpPost_Operation_Batch_Invalid_Argument(string path)
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var batch = new List<ClientQueryRequest>
            {
                new ClientQueryRequest
                {
                    Query =
                    @"
                    query getHero {
                        hero(episode: EMPIRE) {
                            id @export
                        }
                    }

                    query getHuman {
                        human(id: $id) {
                            name
                        }
                    }"
                }
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(
                    batch,
                    path);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);
            byte[] json = await message.Content.ReadAsByteArrayAsync();
            Utf8GraphQLRequestParser.ParseJson(json).MatchSnapshot();
        }
    }
}
