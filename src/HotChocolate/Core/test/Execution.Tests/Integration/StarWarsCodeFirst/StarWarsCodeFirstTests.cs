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
        var snapshot = Snapshot.Create();
        var executor = await CreateExecutorAsync();

        // act
        var schema = executor.Schema.ToString();
        snapshot.Add(schema);

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GetHeroName()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
            """
            {
              hero {
                name
              }
            }
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgFieldExample()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgFieldArgumentExample1()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
            """
            {
              human(id: "1000") {
                name
                height
              }
            }
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgFieldArgumentExample2()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
            """
            {
              human(id: "1000") {
                name
                height(unit: FOOT)
              }
            }
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgAliasExample()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
            """
            {
              empireHero: hero(episode: EMPIRE) {
                name
              }
              jediHero: hero(episode: JEDI) {
                name
              }
            }
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgFragmentExample()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgOperationNameExample()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgVariableExample()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgVariableWithDefaultValueExample()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgDirectiveIncludeExample1()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgDirectiveIncludeExample2()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgDirectiveSkipExample1()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgDirectiveSkipExample2()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgDirectiveSkipExample1WithPlainClrVarTypes()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgMutationExample()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgMutationIgnoreAdditionalInputFieldsExample()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgTwoMutationsExample()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgMutationExample_With_ValueVariables()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgInlineFragmentExample1()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgInlineFragmentExample2()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task GraphQLOrgMetaFieldAndUnionExample()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task NonNullListVariableValues()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task ConditionalInlineFragment()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
            """
            {
              heroes(episodes: [EMPIRE]) {
                name
                ... @include(if: true) {
                  height
                }
              }
            }
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task NonNullEnumsSerializeCorrectlyFromVariables()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task EnumValueIsCoercedToListValue()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
            """
            {
              heroes(episodes: EMPIRE) {
                name
              }
            }
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task TypeNameFieldIsCorrectlyExecutedOnInterfaces()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task Execute_ListWithNullValues_ResultContainsNullElement()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task SubscribeToReview()
    {
        // arrange
        var snapshot = Snapshot.Create();
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

        snapshot.Add(eventResult);

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task SubscribeToReview_WithInlineFragment()
    {
        // arrange
        var snapshot = Snapshot.Create();
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

        snapshot.Add(eventResult);

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task SubscribeToReview_FragmentDefinition()
    {
        // arrange
        var snapshot = Snapshot.Create();
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

        snapshot.Add(eventResult);

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task SubscribeToReview_With_Variables()
    {
        // arrange
        var snapshot = Snapshot.Create();
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

        snapshot.Add(eventResult);

        // assert
        snapshot.Match();
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
        // arrange
        var snapshot = Snapshot.Create(postFix: ifValue);

        // act
        snapshot.Add(await ExpectValid(
            $$"""
            {
              human(id: "1000") {
                name @include(if: {{ifValue}})
                height
              }
            }
            """));

        // assert
        snapshot.Match();
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task Include_With_Variable(bool ifValue)
    {
        // arrange
        var snapshot = Snapshot.Create(postFix: ifValue.ToString());

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [InlineData("true")]
    [InlineData("false")]
    [Theory]
    public async Task Skip_With_Literal(string ifValue)
    {
        // arrange
        var snapshot = Snapshot.Create(postFix: ifValue);

        // act
        snapshot.Add(await ExpectValid(
            $$"""
            {
              human(id: "1000") {
                name @skip(if: {{ifValue}})
                height
              }
            }
            """));

        // assert
        snapshot.Match();
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task Skip_With_Variable(bool ifValue)
    {
        // arrange
        var snapshot = Snapshot.Create(postFix: ifValue.ToString());

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task SkipAll()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task SkipAll_Default_False()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
            """
            query ($if: Boolean! = false) {
              human(id: "1000") @skip(if: $if) {
                name
                height
              }
            }
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task SkipAll_Default_True()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
            """
            query ($if: Boolean! = true) {
              human(id: "1000") @skip(if: $if) {
                name
                height
              }
            }
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task SkipAllSecondLevelFields()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task Ensure_Type_Introspection_Returns_Null_If_Type_Not_Found()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
            """
            query {
              a: __type(name: "Foo") {
                name
              }
              b: __type(name: "Query") {
                name
              }
            }
            """));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task Ensure_Benchmark_Query_GetHeroQuery()
    {
        // arrange
        var snapshot = Snapshot.Create();
        var query = FileResource.Open("GetHeroQuery.graphql");

        // act
        snapshot.Add(await ExpectValid(query));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task Ensure_Benchmark_Query_GetHeroWithFriendsQuery()
    {
        // arrange
        var snapshot = Snapshot.Create();
        var query = FileResource.Open("GetHeroWithFriendsQuery.graphql");

        // act
        snapshot.Add(await ExpectValid(query));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task Ensure_Benchmark_Query_GetTwoHeroesWithFriendsQuery()
    {
        // arrange
        var snapshot = Snapshot.Create();
        var query = FileResource.Open("GetTwoHeroesWithFriendsQuery.graphql");

        // act
        snapshot.Add(await ExpectValid(query));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task Ensure_Benchmark_Query_LargeQuery()
    {
        // arrange
        var snapshot = Snapshot.Create();
        var query = FileResource.Open("LargeQuery.graphql");

        // act
        snapshot.Add(await ExpectValid(query));

        // assert
        snapshot.Match();
    }

    [Fact]
    public async Task NestedFragmentsWithNestedObjectFieldsAndSkip()
    {
        // arrange
        var snapshot = Snapshot.Create();

        // act
        snapshot.Add(await ExpectValid(
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
                """)));

        // assert
        snapshot.Match();
    }
}
