using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Squadron;

namespace HotChocolate.Data.MongoDb.Projections;

public sealed class Issue6291ReproTests(MongoResource resource) : IClassFixture<MongoResource>
{
    private static int _initialized;

    [Fact]
    public async Task Interface_Projection_With_CamelCase_Conventions_Should_Project_Derived_Fields()
    {
        // arrange
        EnsureMongoMappings();

        var collection = resource.CreateCollection<Person>("issue6291_" + Guid.NewGuid().ToString("N"));
        collection.InsertOne(
            new Person
            {
                PersonProp1 = 6,
                PetList =
                [
                    new Dog
                    {
                        AnimalProp1 = 5,
                        AnimalProp2 = "animal-dog",
                        DogProp1 = 11,
                        DogProp2 = "dog-2"
                    },
                    new Cat
                    {
                        AnimalProp1 = 6,
                        AnimalProp2 = "animal-cat",
                        CatProp1 = 22,
                        CatProp2 = "cat-2"
                    }
                ]
            });

        var executor = await new ServiceCollection()
            .AddSingleton<IMongoCollection<Person>>(collection)
            .AddGraphQL()
            .AddMongoDbProjections()
            .AddObjectIdConverters()
            .AddMongoDbFiltering()
            .AddMongoDbSorting()
            .AddMongoDbPagingProviders()
            .AddType<PersonType>()
            .AddType<AnimalType>()
            .AddType<CatType>()
            .AddType<DogType>()
            .AddQueryType<QueryType>()
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              userProfiles {
                items {
                  personProp1
                  petList {
                    __typename
                    animalProp1
                    animalProp2
                    ... on Cat {
                      catProp1
                      catProp2
                    }
                    ... on Dog {
                      dogProp1
                      dogProp2
                    }
                  }
                }
              }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.True(operationResult.Errors is null || operationResult.Errors.Count is 0);

        var json = result.ToJson();
        using var document = JsonDocument.Parse(json);
        var pets = document.RootElement
            .GetProperty("data")
            .GetProperty("userProfiles")
            .GetProperty("items")[0]
            .GetProperty("petList");

        var dog = pets.EnumerateArray().First(x => x.GetProperty("__typename").GetString() == nameof(Dog));
        var cat = pets.EnumerateArray().First(x => x.GetProperty("__typename").GetString() == nameof(Cat));

        Assert.Equal(11, dog.GetProperty("dogProp1").GetInt32());
        Assert.Equal("dog-2", dog.GetProperty("dogProp2").GetString());
        Assert.Equal(22, cat.GetProperty("catProp1").GetInt32());
        Assert.Equal("cat-2", cat.GetProperty("catProp2").GetString());
    }

    private static void EnsureMongoMappings()
    {
        if (Interlocked.Exchange(ref _initialized, 1) is 1)
        {
            return;
        }

        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreIfNullConvention(true),
            new IgnoreExtraElementsConvention(true)
        };

        ConventionRegistry.Register(
            "issue6291_" + Guid.NewGuid().ToString("N"),
            conventionPack,
            t => t == typeof(Person)
                || t == typeof(Animal)
                || t == typeof(Cat)
                || t == typeof(Dog));

        if (!BsonClassMap.IsClassMapRegistered(typeof(Animal)))
        {
            BsonClassMap.RegisterClassMap<Animal>(
                cm =>
                {
                    cm.AutoMap();
                    cm.SetIsRootClass(true);
                    cm.AddKnownType(typeof(Cat));
                    cm.AddKnownType(typeof(Dog));
                });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Cat)))
        {
            BsonClassMap.RegisterClassMap<Cat>(cm => cm.AutoMap());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Dog)))
        {
            BsonClassMap.RegisterClassMap<Dog>(cm => cm.AutoMap());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Person)))
        {
            BsonClassMap.RegisterClassMap<Person>(cm => cm.AutoMap());
        }
    }

    private sealed class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
            descriptor
                .Field("userProfiles")
                .Resolve(
                    static context =>
                        context.Service<IMongoCollection<Person>>().AsExecutable())
                .UseOffsetPaging<ObjectType<Person>>(options: new() { IncludeTotalCount = true })
                .UseProjection()
                .UseFiltering()
                .UseSorting();
        }
    }

    private sealed class PersonType : ObjectType<Person>
    {
        protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
            => descriptor.Field(x => x.PetList).Type<ListType<AnimalType>>();
    }

    private sealed class AnimalType : InterfaceType<Animal>;

    private sealed class CatType : ObjectType<Cat>
    {
        protected override void Configure(IObjectTypeDescriptor<Cat> descriptor)
        {
            descriptor.Implements<AnimalType>();
            descriptor
                .Field("_t")
                .Type<StringType>()
                .IsProjected()
                .Resolve(static _ => nameof(Cat));
        }
    }

    private sealed class DogType : ObjectType<Dog>
    {
        protected override void Configure(IObjectTypeDescriptor<Dog> descriptor)
        {
            descriptor.Implements<AnimalType>();
            descriptor
                .Field("_t")
                .Type<StringType>()
                .IsProjected()
                .Resolve(static _ => nameof(Dog));
        }
    }

    private sealed class Person
    {
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

        public int PersonProp1 { get; set; }

        public IReadOnlyList<Animal> PetList { get; set; } = [];
    }

    private abstract class Animal
    {
        public int AnimalProp1 { get; set; }

        public string AnimalProp2 { get; set; } = string.Empty;
    }

    private sealed class Cat : Animal
    {
        public int CatProp1 { get; set; }

        public string CatProp2 { get; set; } = string.Empty;
    }

    private sealed class Dog : Animal
    {
        public int DogProp1 { get; set; }

        public string DogProp2 { get; set; } = string.Empty;
    }
}
