using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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

            Dictionary<string, IValueNode> variables =
                new Dictionary<string, IValueNode>
            {
                { "episode", new EnumValueNode("JEDI") }
            };

            // act
            IExecutionResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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

            Dictionary<string, IValueNode> variables =
                new Dictionary<string, IValueNode>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(false) }
            };

            // act
            IExecutionResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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

            Dictionary<string, IValueNode> variables =
                new Dictionary<string, IValueNode>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(true) }
            };

            // act
            IExecutionResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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

            Dictionary<string, IValueNode> variables =
                new Dictionary<string, IValueNode>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(false) }
            };

            // act
            IExecutionResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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

            Dictionary<string, IValueNode> variables =
                new Dictionary<string, IValueNode>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(true) }
            };

            // act
            IExecutionResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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

            Dictionary<string, IValueNode> variables =
                new Dictionary<string, IValueNode>
            {
                { "ep", new EnumValueNode("JEDI") },
                { "review", new ObjectValueNode(
                        new ObjectFieldNode("stars", new IntValueNode(5)),
                        new ObjectFieldNode("commentary",
                            new StringValueNode("This is a great movie!"))) }
            };

            // act
            IExecutionResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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

            Dictionary<string, IValueNode> variables =
                new Dictionary<string, IValueNode>
            {
                { "ep", new EnumValueNode("JEDI") },
            };

            // act
            IExecutionResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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

            Dictionary<string, IValueNode> variables =
                new Dictionary<string, IValueNode>
            {
                { "ep", new EnumValueNode("EMPIRE") },
            };

            // act
            IExecutionResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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

            Dictionary<string, IValueNode> variables = new Dictionary<string, IValueNode>
            {
                {"ep", new ListValueNode(new[] {new EnumValueNode("EMPIRE")})}
            };

            // act
            IExecutionResult result = schema.Execute(query, variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        private static Schema CreateSchema()
        {
            CharacterRepository repository = new CharacterRepository();
            Dictionary<Type, object> services = new Dictionary<Type, object>();
            services[typeof(CharacterRepository)] = repository;
            services[typeof(Query)] = new Query(repository);
            services[typeof(Mutation)] = new Mutation();


            Func<Type, object> serviceResolver = new Func<Type, object>(
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
                c.RegisterServiceProvider(serviceProvider.Object);
                c.RegisterQueryType<QueryType>();
                c.RegisterMutationType<MutationType>();
                c.RegisterType<HumanType>();
                c.RegisterType<DroidType>();
                c.RegisterType<EpisodeType>();
            });
        }
    }
}
