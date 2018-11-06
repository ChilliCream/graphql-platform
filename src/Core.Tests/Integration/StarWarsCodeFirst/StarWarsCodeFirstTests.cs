using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Subscriptions;
using Moq;
using Xunit;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class StarWarsCodeFirstTests
    {
        [Fact]
        public void GraphQLOrgFieldExample()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
            {
                hero {
                    name
                    # Queries can have comments!
                    friends {
                        name
                    }
                }
            }";

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgFieldArgumentExample1()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
            {
                human(id: ""1000"") {
                    name
                    height
                }
            }";

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgFieldArgumentExample2()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
            {
                human(id: ""1000"") {
                    name
                    height(unit: FOOT)
                }
            }";

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgAliasExample()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
            {
                empireHero: hero(episode: EMPIRE) {
                    name
                }
                jediHero: hero(episode: JEDI) {
                    name
                }
            }";

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgFragmentExample()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgOperationNameExample()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
            query HeroNameAndFriends {
                hero {
                    name
                    friends {
                        name
                    }
                }
            }";

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgVariableExample()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(
                query, variableValues: variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgVariableWithDefaultValueExample()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
            query HeroNameAndFriends($episode: Episode = JEDI) {
                hero(episode: $episode) {
                    name
                    friends {
                        name
                    }
                }
            }";

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgDirectiveIncludeExample1()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(
                query, variableValues: variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgDirectiveIncludeExample2()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(
                query, variableValues: variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgDirectiveSkipExample1()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(
                query, variableValues: variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgDirectiveSkipExample2()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(
                query, variableValues: variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgMutationExample()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
            mutation CreateReviewForEpisode($ep: Episode!, $review: ReviewInput!) {
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

            // act
            IExecutionResult result = schema.Execute(
                query, variableValues: variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgInlineFragmentExample1()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(query, variableValues: variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgInlineFragmentExample2()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(query, variableValues: variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgMetaFieldAndUnionExample()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void NonNullListVariableValues()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(query, variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void ConditionalInlineFragment()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
            {
                heroes(episodes: [EMPIRE]) {
                    name
                    ... @include(if: true) {
                        height
                    }
                }
            }";

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void NonNullEnumsSerializeCorrectlyFromVariables()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
                query getHero($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }";

            var variables = new Dictionary<string, object>();
            variables["episode"] = new StringValueNode("NEWHOPE");

            // act
            IExecutionResult result = schema.Execute(query, variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void EnumValueIsCoercedToListValue()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
            {
                heroes(episodes: EMPIRE) {
                    name
                }
            }";

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void TypeNameFieldIsCorrectlyExecutedOnInterfaces()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void Execute_ListWithNullValues_ResultContainsNullElement()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
            query {
                human(id: ""1001"") {
                    id
                    name
                    otherHuman {
                        name
                    }
                }
            }";

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public async Task SubscribeToReview()
        {
            // arrange
            Schema schema = CreateSchema();

            // act
            IResponseStream responseStream =
                (IResponseStream)await schema.ExecuteAsync(
                    "subscription { onCreateReview(episode: NEWHOPE) " +
                    "{ stars } }");

            // assert
            IExecutionResult result = await schema.ExecuteAsync(@"
                mutation {
                    createReview(episode: NEWHOPE,
                        review: { stars: 5 commentary: ""foo"" }) {
                        stars
                        commentary
                    }
                }");

            IQueryExecutionResult eventResult;
            using (var cts = new CancellationTokenSource(2000))
            {
                eventResult = await responseStream.ReadAsync();
            }

            eventResult.Snapshot();
        }

        [Fact]
        public void ExecutionDepthShouldNotLeadToEmptyObects()
        {
            // arrange
            Schema schema = CreateSchema(3);
            string query = @"
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

            // act
            IExecutionResult result = schema.Execute(query);

            // assert
            result.Snapshot();
        }

        private static Schema CreateSchema() => CreateSchema(20);

        private static Schema CreateSchema(int executionDepth)
        {
            var repository = new CharacterRepository();
            var eventRegistry = new InMemoryEventRegistry();

            var services = new Dictionary<Type, object>();
            services[typeof(CharacterRepository)] = repository;
            services[typeof(Query)] = new Query(repository);
            services[typeof(Mutation)] = new Mutation();
            services[typeof(Subscription)] = new Subscription();
            services[typeof(IEventSender)] = eventRegistry;
            services[typeof(IEventRegistry)] = eventRegistry;

            var serviceResolver = new Func<Type, object>(
                t =>
                {
                    if (services.TryGetValue(t, out object s))
                    {
                        return s;
                    }
                    return null;
                });

            Mock<IServiceProvider> serviceProvider =
                new Mock<IServiceProvider>(MockBehavior.Strict);

            serviceProvider.Setup(t => t.GetService(It.IsAny<Type>()))
                .Returns(serviceResolver);

            return Schema.Create(c =>
            {
                c.Options.MaxExecutionDepth = executionDepth;

                c.RegisterServiceProvider(serviceProvider.Object);

                c.RegisterDataLoader<HumanDataLoader>();

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
