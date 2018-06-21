using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using Moq;
using Xunit;

namespace HotChocolate.Integration
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
            QueryResult result = schema.Execute(query);

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
            QueryResult result = schema.Execute(query);

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
            QueryResult result = schema.Execute(query);

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
            QueryResult result = schema.Execute(query);

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
            QueryResult result = schema.Execute(query);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }


        [Fact]
        public void GetEpisode5Hero()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = @"
            {
                hero(episode: EMPIRE) {
                    name
                    friends {
                        name
                        friends {
                            name
                        }
                    }
                }

            }";

            // act
            QueryResult result = schema.Execute(query);

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
            QueryResult result = schema.Execute(query);

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
            QueryResult result = schema.Execute(query, variableValues: variables);

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
            QueryResult result = schema.Execute(query);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void GraphQLOrgVariableDirectiveIncludeExample1()
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
            QueryResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void GraphQLOrgVariableDirectiveIncludeExample2()
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
            QueryResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void GraphQLOrgVariableDirectiveSkipExample1()
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
            QueryResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void GraphQLOrgVariableDirectiveSkipExample2()
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
            QueryResult result = schema.Execute(query, variableValues: variables);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        private static Schema CreateSchema()
        {
            CharacterRepository repository = new CharacterRepository();
            Dictionary<Type, object> services = new Dictionary<Type, object>();
            services[typeof(CharacterRepository)] = repository;
            services[typeof(Query)] = new Query(repository);

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
                c.RegisterType<HumanType>();
                c.RegisterType<DroidType>();
                c.RegisterType<EpisodeType>();
            });
        }
    }
}
