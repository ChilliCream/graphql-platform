using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Tests;
using Xunit.Abstractions;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.StarWarsCodeFirst;

public class StarWarsCodeFirstTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Schema()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var schema = executor.Schema.ToString();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task GetHeroName()
    {
        await ExpectValid(
            """
            {
              hero {
                name
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgFieldExample()
    {
        await ExpectValid(
            """
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
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgFieldArgumentExample1()
    {
        await ExpectValid(
            """
            {
              human(id: "1000") {
                name
                height
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgFieldArgumentExample2()
    {
        await ExpectValid(
            """
            {
              human(id: "1000") {
                name
                height(unit: FOOT)
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgAliasExample()
    {
        await ExpectValid(
            """
            {
              empireHero: hero(episode: EMPIRE) {
                name
              }
              jediHero: hero(episode: JEDI) {
                name
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgFragmentExample()
    {
        await ExpectValid(
            """
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
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgOperationNameExample()
    {
        await ExpectValid(
            """
            query HeroNameAndFriends {
              hero {
                name
                friends {
                  nodes {
                    name
                  }
                }
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgVariableExample()
    {
        await ExpectValid(
            """
            query HeroNameAndFriends($episode: Episode) {
              hero(episode: $episode) {
                name
                friends {
                  nodes {
                    name
                  }
                }
              }
            }
            """,
            request: c => c.SetVariableValues(
                """
                {
                  "episode": "JEDI"
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgVariableWithDefaultValueExample()
    {
        await ExpectValid(
            """
            query HeroNameAndFriends($episode: Episode = JEDI) {
              hero(episode: $episode) {
                name
                friends {
                  nodes {
                    name
                  }
                }
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgDirectiveIncludeExample1()
    {
        await ExpectValid(
            """
            query Hero($episode: Episode, $withFriends: Boolean!) {
              hero(episode: $episode) {
                name
                friends @include(if: $withFriends) {
                  nodes {
                    name
                  }
                }
              }
            }
            """,
            request: c => c.SetVariableValues(
                """
                {
                  "episode": "JEDI",
                  "withFriends": false
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgDirectiveIncludeExample2()
    {
        await ExpectValid(
            """
            query Hero($episode: Episode, $withFriends: Boolean!) {
              hero(episode: $episode) {
                name
                friends @include(if: $withFriends) {
                  nodes {
                    name
                  }
                }
              }
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "episode": "JEDI",
                  "withFriends": true
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgDirectiveSkipExample1()
    {
        await ExpectValid(
            """
            query Hero($episode: Episode, $withFriends: Boolean!) {
              hero(episode: $episode) {
                name
                friends @skip(if: $withFriends) {
                  nodes {
                    name
                  }
                }
              }
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "episode": "JEDI",
                  "withFriends": false
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgDirectiveSkipExample2()
    {
        await ExpectValid(
            """
            query Hero($episode: Episode, $withFriends: Boolean!) {
              hero(episode: $episode) {
                name
                friends @skip(if: $withFriends) {
                  nodes {
                    name
                  }
                }
              }
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "episode": "JEDI",
                  "withFriends": true
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgDirectiveSkipExample1WithPlainClrVarTypes()
    {
        await ExpectValid(
            """
            query Hero($episode: Episode, $withFriends: Boolean!) {
              hero(episode: $episode) {
                name
                friends @skip(if: $withFriends) {
                  nodes {
                    name
                  }
                }
              }
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "episode": "JEDI",
                  "withFriends": false
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgMutationExample()
    {
        await ExpectValid(
            """
            mutation CreateReviewForEpisode($ep: Episode!, $review: ReviewInput!) {
              createReview(episode: $ep, review: $review) {
                stars
                commentary
              }
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "ep": "JEDI",
                  "review": {
                    "stars": 5,
                    "commentary": "This is a great movie!"
                  }
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgMutationIgnoreAdditionalInputFieldsExample()
    {
        await ExpectValid(
            """
            mutation CreateReviewForEpisode($ep: Episode!, $review: ReviewInput!) {
              createReview(episode: $ep, review: $review) {
                stars
                commentary
              }
            }
            """,
            configure: c =>
            {
                c.AddInputParser(options => options.IgnoreAdditionalInputFields = true);
                AddDefaultConfiguration(c);
            },
            request: r => r.SetVariableValues(
                """
                {
                  "ep": "JEDI",
                  "review": {
                    "foo": 1,
                    "ignoreMe": "ignored",
                    "stars": 5,
                    "commentary": "This is a great movie!"
                  }
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgTwoMutationsExample()
    {
        await ExpectValid(
            """
            mutation CreateReviewForEpisode($ep: Episode!, $ep2: Episode!, $review: ReviewInput!) {
              createReview(episode: $ep, review: $review) {
                stars
                commentary
              }
              b: createReview(episode: $ep2, review: $review) {
                stars
                commentary
              }
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "ep": "JEDI",
                  "ep2": "JEDI",
                  "review": {
                    "stars": 5,
                    "commentary": "This is a great movie!"
                  }
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgMutationExample_With_ValueVariables()
    {
        await ExpectValid(
            """
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
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "ep": "JEDI",
                  "stars": 5,
                  "commentary": "This is a great movie!"
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgInlineFragmentExample1()
    {
        await ExpectValid(
            """
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
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "ep": "JEDI"
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgInlineFragmentExample2()
    {
        await ExpectValid(
            """
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
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "ep": "EMPIRE"
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLOrgMetaFieldAndUnionExample()
    {
        await ExpectValid(
            """
            {
              search(text: "an") {
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
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NonNullListVariableValues()
    {
        await ExpectValid(
            """
            query op($ep: [Episode!]!) {
              heroes(episodes: $ep) {
                name
              }
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "ep": ["EMPIRE"]
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ConditionalInlineFragment()
    {
        await ExpectValid(
            """
            {
              heroes(episodes: [EMPIRE]) {
                name
                ... @include(if: true) {
                  height
                }
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NonNullEnumsSerializeCorrectlyFromVariables()
    {
        await ExpectValid(
            """
            query getHero($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "episode": "NEW_HOPE"
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task EnumValueIsCoercedToListValue()
    {
        await ExpectValid(
            """
            {
              heroes(episodes: EMPIRE) {
                name
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task TypeNameFieldIsCorrectlyExecutedOnInterfaces()
    {
        await ExpectValid(
            """
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
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Execute_ListWithNullValues_ResultContainsNullElement()
    {
        await ExpectValid(
            """
            query {
              human(id: "1001") {
                id
                name
                otherHuman {
                  name
                }
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SubscribeToReview()
    {
        // arrange
        var executor = await CreateExecutorAsync(output: output);

        // act
        var subscriptionResult =
            (IResponseStream)await executor.ExecuteAsync(
                """
                subscription {
                  onReview(episode: NEW_HOPE) {
                    stars
                  }
                }
                """);

        var results = subscriptionResult.ReadResultsAsync();

        // assert
        await executor.ExecuteAsync(
            """
            mutation {
              createReview(episode: NEW_HOPE, review: { stars: 5 commentary: "foo" }) {
                stars
                commentary
              }
            }
            """);

        OperationResult? eventResult = null;

        using (var cts = new CancellationTokenSource(2000))
        {
            await foreach (var queryResult in results.WithCancellation(cts.Token)
                .ConfigureAwait(false))
            {
                eventResult = queryResult;
                break;
            }
        }

        eventResult?.MatchSnapshot();
    }

    [Fact]
    public async Task SubscribeToReview_WithInlineFragment()
    {
        // arrange
        var executor = await CreateExecutorAsync(output: output);

        // act
        var subscriptionResult =
            (IResponseStream)await executor.ExecuteAsync(
                """
                subscription {
                  onReview(episode: NEW_HOPE) {
                    ... on Review {
                      stars
                    }
                  }
                }
                """);

        // assert
        await executor.ExecuteAsync(
            """
            mutation {
              createReview(episode: NEW_HOPE, review: { stars: 5 commentary: "foo" }) {
                stars
                commentary
              }
            }
            """);

        OperationResult? eventResult = null;

        using (var cts = new CancellationTokenSource(2000))
        {
            await foreach (var queryResult in
                subscriptionResult.ReadResultsAsync().WithCancellation(cts.Token))
            {
                eventResult = queryResult;
                break;
            }
        }

        eventResult?.MatchSnapshot();
    }

    [Fact]
    public async Task SubscribeToReview_FragmentDefinition()
    {
        // arrange
        var executor = await CreateExecutorAsync(output: output);

        // act
        var subscriptionResult =
            (IResponseStream)await executor.ExecuteAsync(
                """
                subscription {
                  onReview(episode: NEW_HOPE) {
                    ... SomeFrag
                  }
                }

                fragment SomeFrag on Review {
                  stars
                }
                """);

        // assert
        await executor.ExecuteAsync(
            """
            mutation {
              createReview(episode: NEW_HOPE, review: { stars: 5 commentary: "foo" }) {
                stars
                commentary
              }
            }
            """);

        OperationResult? eventResult = null;

        using (var cts = new CancellationTokenSource(2000))
        {
            await foreach (var queryResult in
                subscriptionResult.ReadResultsAsync().WithCancellation(cts.Token))
            {
                eventResult = queryResult;
                break;
            }
        }

        eventResult?.MatchSnapshot();
    }

    [Fact]
    public async Task SubscribeToReview_With_Variables()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var subscriptionResult =
            (IResponseStream)await executor.ExecuteAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument(
                        """
                        subscription ($ep: Episode!) {
                          onReview(episode: $ep) {
                            stars
                          }
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                          "ep": "NEW_HOPE"
                        }
                        """)
                    .Build());

        // assert
        await executor.ExecuteAsync(
            """
            mutation {
              createReview(episode: NEW_HOPE, review: { stars: 5 commentary: "foo" }) {
                stars
                commentary
              }
            }
            """);

        OperationResult? eventResult = null;

        using (var cts = new CancellationTokenSource(2000))
        {
            await foreach (var queryResult in
                subscriptionResult.ReadResultsAsync().WithCancellation(cts.Token))
            {
                eventResult = queryResult;
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
        await ExpectError(
            """
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
            }
            """,
            configure: c =>
            {
                AddDefaultConfiguration(c);
                c.AddMaxExecutionDepthRule(3);
            });
    }

    [Fact]
    public async Task OverrideExecutionDepth()
    {
        await ExpectValid(
            """
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
            }
            """,
            configure: c =>
            {
                AddDefaultConfiguration(c);
                c.AddMaxExecutionDepthRule(3, allowRequestOverrides: true);
            },
            request: r => r.SetMaximumAllowedExecutionDepth(100));
    }

    [Fact]
    public async Task SkipExecutionDepth()
    {
        await ExpectValid(
            """
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
            }
            """,
            configure: c =>
            {
                AddDefaultConfiguration(c);
                c.AddMaxExecutionDepthRule(3, allowRequestOverrides: true);
            },
            request: r => r.SkipExecutionDepthAnalysis());
    }

    // this test ensures that overridden depth validations are not cached.
    [Fact]
    public async Task Depth_Analysis_Overrides_Are_Not_Cached()
    {
        // arrange
        const string queryText =
            """
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
            }
            """;

        var configurationA = new TestConfiguration
        {
            ConfigureRequest = r => r.SkipExecutionDepthAnalysis()
        };
        var configurationB = new TestConfiguration
        {
            ConfigureRequest = _ => { }
        };
        var executor = await CreateExecutorAsync(
            c =>
            {
                AddDefaultConfiguration(c);
                c.AddMaxExecutionDepthRule(3, allowRequestOverrides: true);
            });
        var requestA = CreateRequest(configurationA, queryText);
        var requestB = CreateRequest(configurationB, queryText);

        // act
        var resultA = await executor.ExecuteAsync(requestA);
        var resultB = await executor.ExecuteAsync(requestB);

        // assert
        Assert.Empty(Assert.IsType<OperationResult>(resultA).Errors);
        Assert.NotNull(Assert.IsType<OperationResult>(resultB).Errors);
    }

    [Fact]
    public async Task Execution_Depth_Is_Skipped_For_Introspection()
    {
        await ExpectValid(
            """
            query {
              __schema {
                types {
                  fields {
                    type {
                      kind
                      name
                      ofType {
                        kind
                        name
                        ofType {
                          kind
                          name
                          ofType {
                            kind
                            name
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """,
            configure: c =>
            {
                AddDefaultConfiguration(c);
                c.AddMaxExecutionDepthRule(3, skipIntrospectionFields: true);
            });
    }

    [InlineData("true")]
    [InlineData("false")]
    [Theory]
    public async Task Include_With_Literal(string ifValue)
    {
        await ExpectValid(
            $$"""
            {
              human(id: "1000") {
                name @include(if: {{ifValue}})
                height
              }
            }
            """)
            .MatchSnapshotAsync(postFix: ifValue);
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task Include_With_Variable(bool ifValue)
    {
        await ExpectValid(
            """
            query ($if: Boolean!) {
              human(id: "1000") {
                name @include(if: $if)
                height
              }
            }
            """,
            request: r => r.SetVariableValues(
                $$"""
                {
                  "if": {{ifValue.ToString().ToLowerInvariant()}}
                }
                """))
            .MatchSnapshotAsync(postFix: ifValue);
    }

    [InlineData("true")]
    [InlineData("false")]
    [Theory]
    public async Task Skip_With_Literal(string ifValue)
    {
        await ExpectValid(
            $$"""
            {
              human(id: "1000") {
                name @skip(if: {{ifValue}})
                height
              }
            }
            """)
            .MatchSnapshotAsync(postFix: ifValue);
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task Skip_With_Variable(bool ifValue)
    {
        await ExpectValid(
            """
            query ($if: Boolean!) {
              human(id: "1000") {
                name @skip(if: $if)
                height
              }
            }
            """,
            request: r => r.SetVariableValues(
                $$"""
                {
                  "if": {{ifValue.ToString().ToLowerInvariant()}}
                }
                """))
            .MatchSnapshotAsync(postFix: ifValue);
    }

    [Fact]
    public async Task SkipAll()
    {
        await ExpectValid(
            """
            query ($if: Boolean!) {
              human(id: "1000") @skip(if: $if) {
                name
                height
              }
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "if": true
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SkipAll_Default_False()
    {
        await ExpectValid(
            """
            query ($if: Boolean! = false) {
              human(id: "1000") @skip(if: $if) {
                name
                height
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SkipAll_Default_True()
    {
        await ExpectValid(
            """
            query ($if: Boolean! = true) {
              human(id: "1000") @skip(if: $if) {
                name
                height
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SkipAllSecondLevelFields()
    {
        await ExpectValid(
            """
            query ($if: Boolean!) {
              human(id: "1000") {
                name @skip(if: $if)
              }
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "if": true
                }
                """))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Type_Introspection_Returns_Null_If_Type_Not_Found()
    {
        await ExpectValid(
            """
            query {
              a: __type(name: "Foo") {
                name
              }
              b: __type(name: "Query") {
                name
              }
            }
            """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Benchmark_Query_GetHeroQuery()
    {
        var query = FileResource.Open("GetHeroQuery.graphql");
        await ExpectValid(query).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Benchmark_Query_GetHeroWithFriendsQuery()
    {
        var query = FileResource.Open("GetHeroWithFriendsQuery.graphql");
        await ExpectValid(query).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Benchmark_Query_GetTwoHeroesWithFriendsQuery()
    {
        var query = FileResource.Open("GetTwoHeroesWithFriendsQuery.graphql");
        await ExpectValid(query).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Benchmark_Query_LargeQuery()
    {
        var query = FileResource.Open("LargeQuery.graphql");
        await ExpectValid(query).MatchSnapshotAsync();
    }

    [Fact]
    public async Task NestedFragmentsWithNestedObjectFieldsAndSkip()
    {
        await ExpectValid(
            """
            query ($if: Boolean!) {
              human(id: "1000") {
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
            fragment FriendEdge1 on FriendsEdge {
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
              name
              otherHuman {
                __typename
                name
              }
            }
            """,
            request: r => r.SetVariableValues(
                """
                {
                  "if": true
                }
                """))
            .MatchSnapshotAsync();
    }
}
