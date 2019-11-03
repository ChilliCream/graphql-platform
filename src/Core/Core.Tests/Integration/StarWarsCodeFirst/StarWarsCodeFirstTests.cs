using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.DataLoader;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class StarWarsCodeFirstTests
    {
        [Fact]
        public void GraphQLOrgFieldExample()
        {
            // arrange
            var query = @"
            {
                hero {
                    name
                    # Queries can have comments!
                    friends {
                        name
                    }
                }
            }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgFieldArgumentExample1()
        {
            // arrange
            var query = @"
            {
                human(id: ""1000"") {
                    name
                    height
                }
            }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgFieldArgumentExample2()
        {
            // arrange
            var query = @"
            {
                human(id: ""1000"") {
                    name
                    height(unit: FOOT)
                }
            }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgAliasExample()
        {
            // arrange
            var query = @"
            {
                empireHero: hero(episode: EMPIRE) {
                    name
                }
                jediHero: hero(episode: JEDI) {
                    name
                }
            }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgFragmentExample()
        {
            // arrange
            var query = @"
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
                    name
                }
            }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgOperationNameExample()
        {
            // arrange
            var query = @"
            query HeroNameAndFriends {
                hero {
                    name
                    friends {
                        name
                    }
                }
            }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgVariableExample()
        {
            // arrange
            var query = @"
            query HeroNameAndFriends($episode: Episode) {
                hero(episode: $episode) {
                    name
                    friends {
                        name
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "episode", new EnumValueNode("JEDI") }
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgVariableWithDefaultValueExample()
        {
            // arrange
            var query = @"
            query HeroNameAndFriends($episode: Episode = JEDI) {
                hero(episode: $episode) {
                    name
                    friends {
                        name
                    }
                }
            }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgDirectiveIncludeExample1()
        {
            // arrange
            var query = @"
            query Hero($episode: Episode, $withFriends: Boolean!) {
                hero(episode: $episode) {
                    name
                    friends @include(if: $withFriends) {
                        name
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(false) }
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgDirectiveIncludeExample2()
        {
            // arrange
            var query = @"
            query Hero($episode: Episode, $withFriends: Boolean!) {
                hero(episode: $episode) {
                    name
                    friends @include(if: $withFriends) {
                        name
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(true) }
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgDirectiveSkipExample1()
        {
            // arrange
            var query = @"
            query Hero($episode: Episode, $withFriends: Boolean!) {
                hero(episode: $episode) {
                    name
                    friends @skip(if: $withFriends) {
                        name
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(false) }
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgDirectiveSkipExample1WithPlainClrVarTypes()
        {
            // arrange
            var query = @"
            query Hero($episode: Episode, $withFriends: Boolean!) {
                hero(episode: $episode) {
                    name
                    friends @skip(if: $withFriends) {
                        name
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "episode", "JEDI" },
                { "withFriends", false }
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgDirectiveSkipExample2()
        {
            // arrange
            var query = @"
            query Hero($episode: Episode, $withFriends: Boolean!) {
                hero(episode: $episode) {
                    name
                    friends @skip(if: $withFriends) {
                        name
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(true) }
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgMutationExample()
        {
            // arrange
            var query = @"
            mutation CreateReviewForEpisode(
                $ep: Episode!, $review: ReviewInput!) {
                createReview(episode: $ep, review: $review) {
                    stars
                    commentary
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "ep", new EnumValueNode("JEDI") },
                { "review", new ObjectValueNode(
                        new ObjectFieldNode("stars", new IntValueNode(5)),
                        new ObjectFieldNode("commentary",
                            new StringValueNode("This is a great movie!"))) }
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgMutationExample_With_ValueVariables()
        {
            // arrange
            var query = @"
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
            }";

            var variables = new Dictionary<string, object>
            {
                { "ep", new EnumValueNode("JEDI") },
                { "stars", new IntValueNode(5) },
                { "commentary", new StringValueNode("This is a great movie!") }
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgInlineFragmentExample1()
        {
            // arrange
            var query = @"
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
            }";

            var variables = new Dictionary<string, object>
            {
                { "ep", new EnumValueNode("JEDI") },
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgInlineFragmentExample2()
        {
            // arrange
            var query = @"
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
            }";

            var variables = new Dictionary<string, object>
            {
                { "ep", new EnumValueNode("EMPIRE") },
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void GraphQLOrgMetaFieldAndUnionExample()
        {
            // arrange
            var query = @"
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
            }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void NonNullListVariableValues()
        {
            // arrange
            var query = @"
            query op($ep: [Episode!]!)
            {
                heroes(episodes: $ep) {
                    name
                }
            }";

            var variables = new Dictionary<string, object>
            {
                {"ep", new ListValueNode(new[] {new EnumValueNode("EMPIRE")})}
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void ConditionalInlineFragment()
        {
            // arrange
            var query = @"
            {
                heroes(episodes: [EMPIRE]) {
                    name
                    ... @include(if: true) {
                        height
                    }
                }
            }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void NonNullEnumsSerializeCorrectlyFromVariables()
        {
            // arrange
            var query = @"
                query getHero($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }";

            var variables = new Dictionary<string, object>
            {
                ["episode"] = "NEWHOPE"
            };

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void EnumValueIsCoercedToListValue()
        {
            // arrange
            var query = @"
            {
                heroes(episodes: EMPIRE) {
                    name
                }
            }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void TypeNameFieldIsCorrectlyExecutedOnInterfaces()
        {
            // arrange
            var query = @"
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
                }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_ListWithNullValues_ResultContainsNullElement()
        {
            // arrange
            var query = @"
            query {
                human(id: ""1001"") {
                    id
                    name
                    otherHuman {
                        name
                    }
                }
            }";

            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

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

            IReadOnlyQueryResult eventResult;
            using (var cts = new CancellationTokenSource(2000))
            {
                eventResult = await responseStream.ReadAsync(cts.Token);
            }

            eventResult.MatchSnapshot();
        }

        [Fact]
        public void ExecutionDepthShouldNotLeadToEmptyObects()
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

            IQueryExecutor executor = CreateSchema().MakeExecutable(
                new QueryExecutionOptions { MaxExecutionDepth = 3 });

            // act
            IExecutionResult result = executor.Execute(query);

            // assert
            result.MatchSnapshot();
        }

        private static Schema CreateSchema()
        {
            var eventManager = new InMemoryEventRegistry();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<CharacterRepository>();
            serviceCollection.AddSingleton<IEventSender>(eventManager);
            serviceCollection.AddSingleton<IEventRegistry>(eventManager);
            serviceCollection.AddDataLoaderRegistry();
            serviceCollection.AddSingleton<Query>();
            serviceCollection.AddSingleton<Mutation>();
            serviceCollection.AddSingleton<Subscription>();

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            services.GetRequiredService<IDataLoaderRegistry>()
                .Register<HumanDataLoader>();

            return Schema.Create(c =>
            {
                c.RegisterServiceProvider(services);

                c.RegisterQueryType<QueryType>();
                c.RegisterMutationType<MutationType>();
                c.RegisterSubscriptionType<SubscriptionType>();

                c.RegisterType<HumanType>();
                c.RegisterType<DroidType>();
                c.RegisterType<EpisodeType>();
            });
        }
    }
}
