using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.DataLoader;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Subscriptions;
using HotChocolate.Utilities;
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query, variables);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query, variables);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query, variables);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query, variables);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query, variables);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query, variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void GraphQLOrgMutationExample()
        {
            // arrange
            var query = @"
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query, variables);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query, variables);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query, variables);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query, variables);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query, variables);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact(Skip = "waiting for greendonut")]
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

            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
        }

        [Fact]
        public async Task SubscribeToReview()
        {
            // arrange
            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            var responseStream =
                (IResponseStream)await executer.ExecuteAsync(
                    "subscription { onCreateReview(episode: NEWHOPE) " +
                    "{ stars } }");

            // assert
            IExecutionResult result = await executer.ExecuteAsync(@"
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
                eventResult = await responseStream.ReadAsync();
            }

            eventResult.Snapshot();
        }

        [Fact(Skip = "This test is not stable.")]
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

            IQueryExecuter executer = CreateSchema().MakeExecutable(
                new QueryExecutionOptions { MaxExecutionDepth = 3 });

            // act
            IExecutionResult result = executer.Execute(query);

            // assert
            result.Snapshot();
        }

        private static Schema CreateSchema()
        {
            var repository = new CharacterRepository();
            var eventRegistry = new InMemoryEventRegistry();
            var registry = new DataLoaderRegistry(new EmptyServiceProvider());

            var services = new Dictionary<Type, object>
            {
                [typeof(CharacterRepository)] = repository,
                [typeof(Query)] = new Query(repository),
                [typeof(Mutation)] = new Mutation(),
                [typeof(Subscription)] = new Subscription(),
                [typeof(IEventSender)] = eventRegistry,
                [typeof(IEventRegistry)] = eventRegistry,
                [typeof(IDataLoaderRegistry)] = registry
            };

            var serviceResolver = new Func<Type, object>(
                t =>
                {
                    if (services.TryGetValue(t, out var s))
                    {
                        return s;
                    }
                    return null;
                });

            var serviceProvider = new Mock<IServiceProvider>(
                MockBehavior.Strict);

            serviceProvider.Setup(t => t.GetService(It.IsAny<Type>()))
                    .Returns(serviceResolver);

            registry.Register(typeof(HumanDataLoader).FullName,
                s => new HumanDataLoader(repository));

            return Schema.Create(c =>
            {
                c.RegisterServiceProvider(serviceProvider.Object);

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
