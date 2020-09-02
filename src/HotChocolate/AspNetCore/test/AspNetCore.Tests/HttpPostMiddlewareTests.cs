using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using HotChocolate.AspNetCore.Utilities;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class HttpPostMiddlewareTests : ServerTestBase
    {
        public HttpPostMiddlewareTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Simple_IsAlive_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.PostAsync(
                new ClientQueryRequest { Query = "{ __typename }" })
                .ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Simple_IsAlive_Test_On_Non_GraphQL_Path()
        {
            // arrange
            TestServer server = CreateStarWarsServer("/foo");

            // act
            ClientQueryResult result = await server.PostAsync(
                    new ClientQueryRequest { Query = "{ __typename }" },
                    "/foo")
                .ConfigureAwait(false);

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
                await server.PostAsync(new ClientQueryRequest
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
                await server.PostAsync(new ClientQueryRequest
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
                await server.PostAsync(new ClientQueryRequest
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
                await server.PostAsync(new ClientQueryRequest
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
                await server.PostAsync(new ClientQueryRequest
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
                await server.PostAsync(new ClientQueryRequest
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
                await server.PostAsync(new ClientQueryRequest
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
                await server.PostAsync(new ClientQueryRequest
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
                await server.PostAsync(new ClientQueryRequest
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
                await server.PostAsync(new ClientQueryRequest
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
                await server.PostAsync(new ClientQueryRequest
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

        [Fact]
        public async Task SingleRequest_Incomplete()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.PostAsync("{ \"query\":    ");

            // assert
            result.MatchSnapshot();
        }

        [InlineData("{}")]
        [InlineData("{ }")]
        [InlineData("{\n}")]
        [InlineData("{\r\n}")]
        [Theory]
        public async Task SingleRequest_Empty(string request)
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.PostAsync(request);

            // assert
            result.MatchSnapshot();
        }

        [InlineData("[]")]
        [InlineData("[ ]")]
        [InlineData("[\n]")]
        [InlineData("[\r\n]")]
        [Theory]
        public async Task BatchRequest_Empty(string request)
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.PostAsync(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task EmptyRequest()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.PostAsync(string.Empty);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Ensure_Middleware_Mapping()
        {
            // arrange
            TestServer server = CreateStarWarsServer("/foo");

            // act
            ClientQueryResult result = await server.PostAsync(string.Empty);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task BatchRequest_GetHero_And_GetHuman()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            IReadOnlyList<ClientQueryResult> result =
                await server.PostAsync(new List<ClientQueryRequest>
                {
                    new ClientQueryRequest
                    {
                        Query = @"
                        query getHero {
                            hero(episode: EMPIRE) {
                                id @export
                            }
                        }"
                    },
                    new ClientQueryRequest
                    {
                        Query = @"
                        query getHuman {
                            human(id: $id) {
                                name
                            }
                        }"
                    }
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task OperationBatchRequest_GetHero_And_GetHuman()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            IReadOnlyList<ClientQueryResult> result =
                await server.PostOperationAsync(
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
                    },
                    "getHero, getHuman");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task OperationBatchRequest_Invalid_BatchingParameter_1()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            IReadOnlyList<ClientQueryResult> result =
                await server.PostOperationAsync(
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
                    },
                    "getHero",
                    createOperationParameter: s => "batchOperations=" + s);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task OperationBatchRequest_Invalid_BatchingParameter_2()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            IReadOnlyList<ClientQueryResult> result =
                await server.PostOperationAsync(
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
                    },
                    "getHero, getHuman",
                    createOperationParameter: s => "batchOperations=[" + s);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task OperationBatchRequest_Invalid_BatchingParameter_3()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            IReadOnlyList<ClientQueryResult> result =
                await server.PostOperationAsync(
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
                    },
                    "getHero, getHuman",
                    createOperationParameter: s => "batchOperations=" + s);

            // assert
            result.MatchSnapshot();
        }
    }
}
