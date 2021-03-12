using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.Tests;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.StarWarsCodeFirst
{
    public class StarWarsCodeFirstTests
    {
        [Fact]
        public async Task Schema()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync();

            // act
            var schema = executor.Schema.Print();

            // assert
            schema.MatchSnapshot();
        }

        [Fact]
        public async Task GetHeroName()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                {
                    hero {
                        name
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgFieldExample()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                {
                    hero {
                        name
                        # Queries can have comments!
                        friends {
                            nodes {
                                name
                            }
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgFieldArgumentExample1()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                {
                    human(id: ""1000"") {
                        name
                        height
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgFieldArgumentExample2()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                {
                    human(id: ""1000"") {
                        name
                        height(unit: FOOT)
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgAliasExample()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                {
                    empireHero: hero(episode: EMPIRE) {
                        name
                    }
                    jediHero: hero(episode: JEDI) {
                        name
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgFragmentExample()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                {
                    leftComparison: hero(episode: EMPIRE) {
                        ...comparisonFields
                    }
                    rightComparison: hero(episode: JEDI) {
                        ...comparisonFields
                    }
                }

                fragment comparisonFields on Character {
                    name
                    appearsIn
                    friends {
                        nodes {
                            name
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgOperationNameExample()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query HeroNameAndFriends {
                    hero {
                        name
                        friends {
                            nodes {
                                name
                            }
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgVariableExample()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query HeroNameAndFriends($episode: Episode) {
                    hero(episode: $episode) {
                        name
                        friends {
                            nodes {
                                name
                            }
                        }
                    }
                }",
                request: c => c.SetVariableValue("episode", new EnumValueNode("JEDI")))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgVariableWithDefaultValueExample()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query HeroNameAndFriends($episode: Episode = JEDI) {
                    hero(episode: $episode) {
                        name
                        friends {
                            nodes {
                                name
                            }
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgDirectiveIncludeExample1()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query Hero($episode: Episode, $withFriends: Boolean!) {
                    hero(episode: $episode) {
                        name
                        friends @include(if: $withFriends) {
                            nodes {
                                name
                            }
                        }
                    }
                }",
                request: c => c
                    .SetVariableValue("episode", new EnumValueNode("JEDI"))
                    .SetVariableValue("withFriends", new BooleanValueNode(false)))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgDirectiveIncludeExample2()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query Hero($episode: Episode, $withFriends: Boolean!) {
                    hero(episode: $episode) {
                        name
                        friends @include(if: $withFriends) {
                            nodes {
                                name
                            }
                        }
                    }
                }",
                request: r => r
                    .SetVariableValue("episode", new EnumValueNode("JEDI"))
                    .SetVariableValue("withFriends", new BooleanValueNode(true)))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgDirectiveSkipExample1()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query Hero($episode: Episode, $withFriends: Boolean!) {
                    hero(episode: $episode) {
                        name
                        friends @skip(if: $withFriends) {
                            nodes {
                                name
                            }
                        }
                    }
                }",
                request: r => r
                    .SetVariableValue("episode", new EnumValueNode("JEDI"))
                    .SetVariableValue("withFriends", new BooleanValueNode(false)))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgDirectiveSkipExample2()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query Hero($episode: Episode, $withFriends: Boolean!) {
                    hero(episode: $episode) {
                        name
                        friends @skip(if: $withFriends) {
                            nodes {
                                name
                            }
                        }
                    }
                }",
                request: r => r
                    .SetVariableValue("episode", new EnumValueNode("JEDI"))
                    .SetVariableValue("withFriends", new BooleanValueNode(true)))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgDirectiveSkipExample1WithPlainClrVarTypes()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query Hero($episode: Episode, $withFriends: Boolean!) {
                    hero(episode: $episode) {
                        name
                        friends @skip(if: $withFriends) {
                            nodes {
                                name
                            }
                        }
                    }
                }",
                request: r => r
                    .SetVariableValue("episode", "JEDI")
                    .SetVariableValue("withFriends", false))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgMutationExample()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                mutation CreateReviewForEpisode(
                    $ep: Episode!, $review: ReviewInput!) {
                    createReview(episode: $ep, review: $review) {
                        stars
                        commentary
                    }
                }",
                request: r => r
                    .SetVariableValue("ep", new EnumValueNode("JEDI"))
                    .SetVariableValue("review", new ObjectValueNode(
                        new ObjectFieldNode("stars", new IntValueNode(5)),
                        new ObjectFieldNode("commentary",
                            new StringValueNode("This is a great movie!")))))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgTwoMutationsExample()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                mutation CreateReviewForEpisode(
                    $ep: Episode!, $ep2: Episode!, $review: ReviewInput!) {
                    createReview(episode: $ep, review: $review) {
                        stars
                        commentary
                    }
                    b: createReview(episode: $ep2, review: $review) {
                        stars
                        commentary
                    }
                }",
                request: r => r
                    .SetVariableValue("ep", new EnumValueNode("JEDI"))
                    .SetVariableValue("ep2", new EnumValueNode("JEDI"))
                    .SetVariableValue("review", new ObjectValueNode(
                        new ObjectFieldNode("stars", new IntValueNode(5)),
                        new ObjectFieldNode("commentary",
                            new StringValueNode("This is a great movie!")))))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgMutationExample_With_ValueVariables()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                mutation CreateReviewForEpisode(
                    $ep: Episode!
                    $stars: Int!
                    $commentary: String!) {
                    createReview(
                        episode: $ep
                        review: { stars: $stars commentary: $commentary }) {
                        stars
                        commentary
                    }
                }",
                request: r => r
                    .SetVariableValue("ep", new EnumValueNode("JEDI"))
                    .SetVariableValue("stars", new IntValueNode(5))
                    .SetVariableValue("commentary", new StringValueNode("This is a great movie!")))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgInlineFragmentExample1()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query HeroForEpisode($ep: Episode!) {
                    hero(episode: $ep) {
                        name
                        ... on Droid {
                            primaryFunction
                        }
                        ... on Human {
                            height
                        }
                    }
                }",
                request: r => r.SetVariableValue("ep", new EnumValueNode("JEDI")))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgInlineFragmentExample2()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query HeroForEpisode($ep: Episode!) {
                    hero(episode: $ep) {
                        name
                        ... on Droid {
                            primaryFunction
                        }
                        ... on Human {
                            height
                        }
                    }
                }",
                request: r => r.SetVariableValue("ep", new EnumValueNode("EMPIRE")))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GraphQLOrgMetaFieldAndUnionExample()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                {
                    search(text: ""an"") {
                        __typename
                        ... on Human {
                            name
                            height
                        }
                        ... on Droid {
                            name
                            primaryFunction
                        }
                        ... on Starship {
                            name
                            length
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task NonNullListVariableValues()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query op($ep: [Episode!]!)
                {
                    heroes(episodes: $ep) {
                        name
                    }
                }",
                request: r => r
                    .SetVariableValue("ep", new ListValueNode(new EnumValueNode("EMPIRE"))))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task ConditionalInlineFragment()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                {
                    heroes(episodes: [EMPIRE]) {
                        name
                        ... @include(if: true) {
                            height
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task NonNullEnumsSerializeCorrectlyFromVariables()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query getHero($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }",
                request: r => r.SetVariableValue("episode", "NEW_HOPE"))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task EnumValueIsCoercedToListValue()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                {
                    heroes(episodes: EMPIRE) {
                        name
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task TypeNameFieldIsCorrectlyExecutedOnInterfaces()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query foo {
                    hero(episode: NEW_HOPE) {
                        __typename
                        id
                        name
                        ... on Human {
                            __typename
                            homePlanet
                        }
                        ... on Droid {
                            __typename
                            primaryFunction
                        }
                        friends {
                            nodes {
                                __typename
                                ... on Human {
                                    __typename
                                    homePlanet
                                }
                                ... on Droid {
                                    __typename
                                    primaryFunction
                                }
                            }
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Execute_ListWithNullValues_ResultContainsNullElement()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query {
                    human(id: ""1001"") {
                        id
                        name
                        otherHuman {
                            name
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task SubscribeToReview()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync();

            // act
            var subscriptionResult =
                (ISubscriptionResult)await executor.ExecuteAsync(
                    "subscription { onReview(episode: NEW_HOPE) " +
                    "{ stars } }");

            // assert
            IExecutionResult result =
                await executor.ExecuteAsync(@"
                    mutation {
                        createReview(episode: NEW_HOPE,
                            review: { stars: 5 commentary: ""foo"" }) {
                            stars
                            commentary
                        }
                    }");

            IReadOnlyQueryResult eventResult = null;

            using (var cts = new CancellationTokenSource(2000))
            {
                await foreach (IQueryResult queryResult in
                    subscriptionResult.ReadResultsAsync().WithCancellation(cts.Token))
                {
                    var item = (IReadOnlyQueryResult) queryResult;
                    eventResult = item;
                    break;
                }
            }

            eventResult?.MatchSnapshot();
        }

        [Fact]
        public async Task SubscribeToReview_With_Variables()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync();

            // act
            var subscriptionResult =
                (ISubscriptionResult)await executor.ExecuteAsync(
                    @"subscription ($ep: Episode!) {
                        onReview(episode: $ep) {
                            stars
                        }
                    }",
                    new Dictionary<string, object> { { "ep", "NEW_HOPE" } },
                    CancellationToken.None);

            // assert
            IExecutionResult result =
                await executor.ExecuteAsync(@"
                    mutation {
                        createReview(episode: NEW_HOPE,
                            review: { stars: 5 commentary: ""foo"" }) {
                            stars
                            commentary
                        }
                    }");

            IReadOnlyQueryResult eventResult = null;

            using (var cts = new CancellationTokenSource(2000))
            {
                await foreach (IQueryResult queryResult in
                    subscriptionResult.ReadResultsAsync().WithCancellation(cts.Token))
                {
                    var item = (IReadOnlyQueryResult) queryResult;
                    eventResult = item;
                    break;
                }
            }

            eventResult?.MatchSnapshot();
        }

        /// <summary>
        /// An error caused by the violating the max execution depth rule should
        /// not lead to partial results.
        /// The result should consist of a single error stating the allowed depth.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ExecutionDepthShouldNotLeadToEmptyObjects()
        {
            Snapshot.FullName();
            await ExpectError(@"
                query ExecutionDepthShouldNotLeadToEmptyObjects {
                    hero(episode: NEW_HOPE) {
                        __typename
                        id
                        name
                        ... on Human {
                            __typename
                            homePlanet
                        }
                        ... on Droid {
                            __typename
                            primaryFunction
                        }
                        friends {
                            nodes {
                                __typename
                                ... on Human {
                                    __typename
                                    homePlanet
                                    friends {
                                        nodes {
                                            __typename
                                        }
                                    }
                                }
                                ... on Droid {
                                    __typename
                                    primaryFunction
                                    friends {
                                        nodes {
                                            __typename
                                        }
                                    }
                                }
                            }
                        }
                    }
                }",
                configure: c =>
                {
                    AddDefaultConfiguration(c);
                    c.AddMaxExecutionDepthRule(3);
                });
        }

        [InlineData("true")]
        [InlineData("false")]
        [Theory]
        public async Task Include_With_Literal(string ifValue)
        {
            Snapshot.FullName(new SnapshotNameExtension(ifValue));
            await ExpectValid($@"
                {{
                    human(id: ""1000"") {{
                        name @include(if: {ifValue})
                        height
                    }}
                }}")
                .MatchSnapshotAsync();
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public async Task Include_With_Variable(bool ifValue)
        {
            Snapshot.FullName(new SnapshotNameExtension(ifValue));
            await ExpectValid($@"
                query ($if: Boolean!) {{
                    human(id: ""1000"") {{
                        name @include(if: $if)
                        height
                    }}
                }}",
                request: r => r.SetVariableValue("if", ifValue))
                .MatchSnapshotAsync();
        }

        [InlineData("true")]
        [InlineData("false")]
        [Theory]
        public async Task Skip_With_Literal(string ifValue)
        {
            Snapshot.FullName(new SnapshotNameExtension(ifValue));
            await ExpectValid($@"
                {{
                    human(id: ""1000"") {{
                        name @skip(if: {ifValue})
                        height
                    }}
                }}")
                .MatchSnapshotAsync();
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public async Task Skip_With_Variable(bool ifValue)
        {
            Snapshot.FullName(new SnapshotNameExtension(ifValue));
            await ExpectValid(@"
                query ($if: Boolean!) {
                    human(id: ""1000"") {
                        name @skip(if: $if)
                        height
                    }
                }",
                request: r=> r.SetVariableValue("if", ifValue))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task SkipAll()
        {
            Snapshot.FullName();

            await ExpectValid(@"
                query ($if: Boolean!) {
                    human(id: ""1000"") @skip(if: $if) {
                        name
                        height
                    }
                }",
                request: r=> r.SetVariableValue("if", true))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task SkipAllSecondLevelFields()
        {
            Snapshot.FullName();

            await ExpectValid(@"
                query ($if: Boolean!) {
                    human(id: ""1000"")  {
                        name @skip(if: $if)
                    }
                }",
                request: r=> r.SetVariableValue("if", true))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Ensure_Type_Introspection_Returns_Null_If_Type_Not_Found()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query {
                    a: __type(name: ""Foo"") {
                        name
                    }
                    b: __type(name: ""Query"") {
                        name
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task NestedFragmentsWithNestedObjectFieldsAndSkip()
        {
            Snapshot.FullName();
            await ExpectValid(@"
                query ($if: Boolean!) {
                    human(id: ""1000"") {
                        ... Human1 @include(if: $if)
                        ... Human2 @skip(if: $if)
                    }
                }
                fragment Human1 on Human {
                    friends {
                        edges {
                            ... FriendEdge1
                        }
                    }
                }
                fragment FriendEdge1 on CharacterEdge {
                    node {
                        __typename
                        friends {
                            nodes {
                                __typename
                                ... Human3
                            }
                        }
                    }
                }
                fragment Human2 on Human {
                    friends {
                        edges {
                            node {
                                __typename
                                ... Human3
                            }
                        }
                    }
                }
                fragment Human3 on Human {
                    name # this works

                    # this is rturned as an empty object but should be populated
                    # interestingly it seems like this passes in this test, but
                    # if run against the StarWars asp.net core sample app, this
                    # is an empty object!
                    otherHuman { 
                      name
                    }
                }
                ",
                request: r => r.SetVariableValue("if", false))
                .MatchSnapshotAsync();
        }
    }
}
