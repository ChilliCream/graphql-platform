using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
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
        public void GetEpisode5HeroWithInlineFragments()
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
                        ... on Droid {
                            primaryFunction
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
        public void GetEpisode5HeroWithFragments()
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
                        ... additional
                    }
                }
            }

            fragment additional on Droid {
                primaryFunction
            }

            fragment additional on Human {
                homePlanet
            }";

            // act
            QueryResult result = schema.Execute(query);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void CompareEpisodeHeros()
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


    public enum Episode
    {
        NewHope,
        Empire,
        Jedi
    }

    public class EpisodeType
        : EnumType<Episode>
    {
    }

    public class CharacterType
        : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Character");
            descriptor.Field("id").Type<NonNullType<StringType>>();
            descriptor.Field("name").Type<StringType>();
            descriptor.Field("friends").Type<ListType<CharacterType>>();
            descriptor.Field("appearsIn").Type<ListType<EpisodeType>>(); ;
        }

        public static IEnumerable<ICharacter> GetCharacter(
            IResolverContext context)
        {
            ICharacter character = context.Parent<ICharacter>();
            CharacterRepository repository = context.Service<CharacterRepository>();
            foreach (string friendId in character.Friends)
            {
                ICharacter friend = repository.GetCharacter(friendId);
                if (friend != null)
                {
                    yield return friend;
                }
            }
        }
    }

    public class HumanType
        : ObjectType<Human>
    {
        protected override void Configure(IObjectTypeDescriptor<Human> descriptor)
        {
            descriptor.Interface<CharacterType>();
            descriptor.Field(t => t.Friends)
                .Type<ListType<CharacterType>>()
                .Resolver(c => CharacterType.GetCharacter(c));
        }
    }

    public class DroidType
        : ObjectType<Droid>
    {
        protected override void Configure(IObjectTypeDescriptor<Droid> descriptor)
        {
            descriptor.Interface<CharacterType>();
            descriptor.Field(t => t.Friends)
                .Type<ListType<CharacterType>>()
                .Resolver(c => CharacterType.GetCharacter(c));
        }
    }

    public class QueryType
        : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.GetHero(default))
                .Type<CharacterType>()
                .Argument("episode", a => a.DefaultValue(Episode.NewHope));
        }
    }

    public class Human
        : ICharacter
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public IReadOnlyList<string> Friends { get; set; }

        public IReadOnlyList<Episode> AppearsIn { get; set; }

        public string HomePlanet { get; set; }
    }

    public class Droid
       : ICharacter
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public IReadOnlyList<string> Friends { get; set; }

        public IReadOnlyList<Episode> AppearsIn { get; set; }

        public string PrimaryFunction { get; set; }
    }

    public interface ICharacter
    {
        string Id { get; }
        string Name { get; }
        IReadOnlyList<string> Friends { get; }
        IReadOnlyList<Episode> AppearsIn { get; }
    }

    public class Query
    {
        private readonly CharacterRepository _repository;

        public Query(CharacterRepository repository)
        {
            _repository = repository
                ?? throw new System.ArgumentNullException(nameof(repository));
        }

        public ICharacter GetHero(Episode episode)
        {
            return _repository.GetHero(episode);
        }

        public Human GetHuman(string id)
        {
            return _repository.GetHuman(id);
        }

        public Droid GetDroid(string id)
        {
            return _repository.GetDroid(id);
        }
    }

    public class CharacterRepository
    {
        private Dictionary<string, ICharacter> _characters;

        public CharacterRepository()
        {
            _characters = CreateCharacters().ToDictionary(t => t.Id);
        }

        public ICharacter GetHero(Episode episode)
        {
            if (episode == Episode.Empire)
            {
                return _characters["1000"];
            }
            return _characters["2001"];
        }

        public ICharacter GetCharacter(string id)
        {
            if (_characters.TryGetValue(id, out ICharacter c))
            {
                return c;
            }
            return null;
        }

        public Human GetHuman(string id)
        {
            if (_characters.TryGetValue(id, out ICharacter c)
                && c is Human h)
            {
                return h;
            }
            return null;
        }

        public Droid GetDroid(string id)
        {
            if (_characters.TryGetValue(id, out ICharacter c)
                && c is Droid d)
            {
                return d;
            }
            return null;
        }

        private static IEnumerable<ICharacter> CreateCharacters()
        {
            yield return new Human
            {
                Id = "1000",
                Name = "Luke Skywalker",
                Friends = new[] { "1002", "1003", "2000", "2001" },
                AppearsIn = new[] { Episode.NewHope, Episode.Empire, Episode.Jedi },
                HomePlanet = "Tatooine"
            };

            yield return new Human
            {
                Id = "1001",
                Name = "Darth Vader",
                Friends = new[] { "1004" },
                AppearsIn = new[] { Episode.NewHope, Episode.Empire, Episode.Jedi },
                HomePlanet = "Tatooine"
            };

            yield return new Human
            {
                Id = "1002",
                Name = "Han Solo",
                Friends = new[] { "1000", "1003", "2001" },
                AppearsIn = new[] { Episode.NewHope, Episode.Empire, Episode.Jedi }
            };

            yield return new Human
            {
                Id = "1003",
                Name = "Leia Organa",
                Friends = new[] { "1000", "1002", "2000", "2001" },
                AppearsIn = new[] { Episode.NewHope, Episode.Empire, Episode.Jedi },
                HomePlanet = "Alderaan"
            };

            yield return new Human
            {
                Id = "1004",
                Name = "Wilhuff Tarkin",
                Friends = new[] { "1001" },
                AppearsIn = new[] { Episode.NewHope }
            };

            yield return new Droid
            {
                Id = "2000",
                Name = "C-3PO",
                Friends = new[] { "1000", "1002", "1003", "2001" },
                AppearsIn = new[] { Episode.NewHope, Episode.Empire, Episode.Jedi },
                PrimaryFunction = "Protocol"
            };

            yield return new Droid
            {
                Id = "2001",
                Name = "R2-D2",
                Friends = new[] { "1000", "1002", "1003" },
                AppearsIn = new[] { Episode.NewHope, Episode.Empire, Episode.Jedi },
                PrimaryFunction = "Astromech"
            };
        }
    }

}
