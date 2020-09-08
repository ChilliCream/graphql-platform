using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.TestHost;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class HttpGetMiddlewareTests : ServerTestBase
    {
        public HttpGetMiddlewareTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Simple_IsAlive_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.GetAsync(
                new ClientQueryRequest { Query = "{ __typename }" });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Simple_IsAlive_Test_On_Non_GraphQL_Path()
        {
            // arrange
            TestServer server = CreateStarWarsServer("/foo");

            // act
            ClientQueryResult result = await server.GetAsync(
                new ClientQueryRequest { Query = "{ __typename }" },
                "/foo");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_GetHeroName()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.GetAsync(new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero {
                            name
                        }
                    }"
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_GetHeroName_Casing_Is_Preserved()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.GetAsync(new ClientQueryRequest
                {
                    Query = @"
                    {
                        HERO: hero {
                            name
                        }
                    }"
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_GetHeroName_With_EnumVariable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.GetAsync(new ClientQueryRequest
                {
                    Query = @"
                    query ($episode: Episode!) {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "episode", "NEW_HOPE" }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_GetHumanName_With_StringVariable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.GetAsync(new ClientQueryRequest
                {
                    Query = @"
                    query h($id: String!) {
                        human(id: $id) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "id", "1000" }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_CreateReviewForEpisode_With_ObjectVariable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.GetAsync(new ClientQueryRequest
                {
                    Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $review: ReviewInput!) {
                        createReview(episode: $ep, review: $review) {
                            stars
                            commentary
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "ep", "EMPIRE" },
                        {
                            "review",
                            new Dictionary<string, object>
                            {
                                { "stars", 5 },
                                { "commentary", "This is a great movie!" },
                            }
                        }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_CreateReviewForEpisode_Omit_NonNull_Variable()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.GetAsync(new ClientQueryRequest
                {
                    Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $review: ReviewInput!) {
                        createReview(episode: $ep, review: $review) {
                            stars
                            commentary
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        {
                            "review",
                            new Dictionary<string, object>
                            {
                                { "stars", 5 },
                                { "commentary", "This is a great movie!" },
                            }
                        }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_CreateReviewForEpisode_Variables_In_ObjectValue()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.GetAsync(new ClientQueryRequest
                {
                    Query = @"
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
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "ep", "EMPIRE" },
                        { "stars", 5 },
                        { "commentary", "This is a great movie!" }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_CreateReviewForEpisode_Variables_Unused()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.GetAsync(new ClientQueryRequest
                {
                    Query = @"
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
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "ep", "EMPIRE" },
                        { "stars", 5 },
                        { "commentary", "This is a great movie!" }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [InlineData("a")]
        [InlineData("b")]
        [Theory]
        public async Task SingleRequest_Execute_Specific_Operation(
            string operationName)
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.GetAsync(new ClientQueryRequest
                {
                    Query = @"
                    query a {
                        a: hero {
                            name
                        }
                    }

                    query b {
                        b: hero {
                            name
                        }
                    }",
                    OperationName = operationName
                });

            // assert
            result.MatchSnapshot(new SnapshotNameExtension(operationName));
        }

        [Fact]
        public async Task SingleRequest_ValidationError()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.GetAsync(new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object>
                    {
                        { "episode", "NEW_HOPE" }
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleRequest_SyntaxError()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result =
                await server.GetAsync(new ClientQueryRequest
                {
                    Query = @"
                    {
                        ähero {
                            name
                        }
                    }"
                });

            // assert
            result.MatchSnapshot();
        }
    }
}
