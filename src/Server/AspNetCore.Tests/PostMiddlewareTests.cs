using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.TestHost;
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
                await server.SendRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
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
                await server.SendRequestAsync(request);

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
                await server.SendRequestAsync(request);

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
                await server.SendRequestAsync(request);

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
                await server.SendRequestAsync(request, "foo");

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
                await server.SendRequestAsync(request, "bar");

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
                await server.SendRequestAsync(request);

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
                await server.SendRequestAsync(request);

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
                await server.SendRequestAsync(request);

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
                await server.SendRequestAsync(request);

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
                await server.SendRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }
    }
}
