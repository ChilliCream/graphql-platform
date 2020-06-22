﻿using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class StarWarsCodeFirstTests
    {
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
                request: r => r.SetVariableValue("episode", "NEWHOPE"))
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
                    hero(episode: NEWHOPE) {
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

        /*
                [Fact]
                public async Task SubscribeToReview()
                {
                    // arrange
                    IQueryExecutor executor = CreateSchema().MakeExecutable();

                    // act
                    var responseStream =
                        (IResponseStream)await executor.ExecuteAsync(
                            "subscription { onCreateReview(episode: NEWHOPE) " +
                            "{ stars } }");

                    // assert
                    IExecutionResult result = await executor.ExecuteAsync(@"
                        mutation {
                            createReview(episode: NEWHOPE,
                                review: { stars: 5 commentary: ""foo"" }) {
                                stars
                                commentary
                            }
                        }");

                    IReadOnlyQueryResult eventResult = null;
                    using (var cts = new CancellationTokenSource(2000))
                    {
                        await foreach (IReadOnlyQueryResult item in
                            responseStream.WithCancellation(cts.Token))
                        {
                            eventResult = item;
                            break;
                        }
                    }

                    eventResult.MatchSnapshot();
                }



                [Fact]
                public void ExecutionDepthShouldNotLeadToEmptyObjects()
                {
                    // arrange
                    var query = @"
                    query ExecutionDepthShouldNotLeadToEmptyObects {
                        hero(episode: NEWHOPE) {
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
                                __typename
                                ... on Human {
                                    __typename
                                    homePlanet
                                    friends {
                                        __typename
                                    }
                                }
                                ... on Droid {
                                    __typename
                                    primaryFunction
                                    friends {
                                        __typename
                                    }
                                }
                            }
                        }
                    }";

                    ISchema schema = CreateSchema();

                    IQueryExecutor executor = CreateSchema().MakeExecutable(
                        new QueryExecutionOptions { MaxExecutionDepth = 3 });

                    // act
                    IExecutionResult result = executor.Execute(query);

                    // assert
                    result.MatchSnapshot();
                }

                [InlineData("true")]
                [InlineData("false")]
                [Theory]
                public void Include_With_Literal(string ifValue)
                {
                    // arrange
                    var query = $@"
                    {{
                        human(id: ""1000"") {{
                            name @include(if: {ifValue})
                            height
                        }}
                    }}";

                    IQueryExecutor executor = CreateSchema().MakeExecutable();

                    // act
                    IExecutionResult result = executor.Execute(query);

                    // assert
                    result.MatchSnapshot(new SnapshotNameExtension(ifValue));
                }

                [InlineData(true)]
                [InlineData(false)]
                [Theory]
                public void Include_With_Variable(bool ifValue)
                {
                    // arrange
                    var query = $@"
                    query ($if: Boolean!) {{
                        human(id: ""1000"") {{
                            name @include(if: $if)
                            height
                        }}
                    }}";

                    IQueryExecutor executor = CreateSchema().MakeExecutable();

                    // act
                    IExecutionResult result = executor.Execute(
                        query,
                        new Dictionary<string, object>
                        {
                            { "if", ifValue }
                        });

                    // assert
                    result.MatchSnapshot(new SnapshotNameExtension(ifValue));
                }

                [InlineData("true")]
                [InlineData("false")]
                [Theory]
                public void Skip_With_Literal(string ifValue)
                {
                    // arrange
                    var query = $@"
                    {{
                        human(id: ""1000"") {{
                            name @skip(if: {ifValue})
                            height
                        }}
                    }}";

                    IQueryExecutor executor = CreateSchema().MakeExecutable();

                    // act
                    IExecutionResult result = executor.Execute(query);

                    // assert
                    result.MatchSnapshot(new SnapshotNameExtension(ifValue));
                }

                [InlineData(true)]
                [InlineData(false)]
                [Theory]
                public void Skip_With_Variable(bool ifValue)
                {
                    // arrange
                    var query = $@"
                    query ($if: Boolean!) {{
                        human(id: ""1000"") {{
                            name @skip(if: $if)
                            height
                        }}
                    }}";

                    IQueryExecutor executor = CreateSchema().MakeExecutable();

                    // act
                    IExecutionResult result = executor.Execute(
                        query,
                        new Dictionary<string, object>
                        {
                            { "if", ifValue }
                        });

                    // assert
                    result.MatchSnapshot(new SnapshotNameExtension(ifValue));
                }

                [Fact]
                public void Ensure_Type_Introspection_Returns_Null_If_Type_Not_Found()
                {
                    // arrange
                    var query = @"
                    query {
                        a: __type(name: ""Foo"") {
                            name
                        }
                        b: __type(name: ""Query"") {
                            name
                        }
                    }";

                    IQueryExecutor executor = CreateSchema().MakeExecutable(
                        new QueryExecutionOptions { MaxExecutionDepth = 3 });

                    // act
                    IExecutionResult result = executor.Execute(query);

                    // assert
                    result.MatchSnapshot();
                }

                */
    }
}
