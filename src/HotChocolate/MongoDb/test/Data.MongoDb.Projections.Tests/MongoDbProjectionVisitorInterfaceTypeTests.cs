using System.Text.Json;
using System.Threading;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Projections;

public class MongoDbProjectionVisitorInterfaceTypeTests(MongoResource resource) : IClassFixture<MongoResource>
{
    private static int s_mongoMappingsInitialized;

    [Fact]
    public async Task Projection_Should_Map_Polymorphic_Fields_And_Include_Discriminator()
    {
        EnsureMongoMappings();

        var executor = CreateSchema(
            resource,
            new Fauna
            {
                Name = "Savannah",
                Animals =
                [
                    new Dog
                    {
                        Name = "Rex",
                        Age = 4,
                        Species = "Canis lupus familiaris",
                        Breed = "Labrador",
                        IsGoodBoy = true
                    },
                    new Cat
                    {
                        Name = "Misty",
                        Age = 3,
                        Species = "Felis catus",
                        IsIndoor = false,
                        LivesUsed = 2
                    },
                    new Bird
                    {
                        Name = "Skye",
                        Age = 1,
                        Species = "Psittaciformes",
                        CanFly = true,
                        WingspanCm = 28
                    }
                ]
            });

        var result = await executor.ExecuteAsync(
            """
            {
              faunas {
                name
                animals {
                  __typename
                  name
                  age
                  species
                  ... on Dog {
                    breed
                    isGoodBoy
                  }
                  ... on Cat {
                    isIndoor
                    livesUsed
                  }
                  ... on Bird {
                    canFly
                    wingspanCm
                  }
                }
              }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        var query = operationResult.ContextData.TryGetValue("query", out var queryValue)
            ? queryValue as string ?? "<missing query>"
            : "<missing query>";
        Assert.True(
            operationResult.Errors is null || operationResult.Errors.Count == 0,
            operationResult.ToJson() + Environment.NewLine + query);

        using var json = JsonDocument.Parse(operationResult.ToJson());
        var animals = json.RootElement
            .GetProperty("data")
            .GetProperty("faunas")[0]
            .GetProperty("animals");

        Assert.Equal("Dog", animals[0].GetProperty("__typename").GetString());
        Assert.Equal("Cat", animals[1].GetProperty("__typename").GetString());
        Assert.Equal("Bird", animals[2].GetProperty("__typename").GetString());
        Assert.Equal(2, animals[1].GetProperty("livesUsed").GetInt32());
        Assert.True(animals[2].GetProperty("canFly").GetBoolean());

        Assert.True(operationResult.ContextData.TryGetValue("query", out queryValue));
        query = Assert.IsType<string>(queryValue);
        Assert.Contains("\"an._t\" : 1", query);
        Assert.Contains("\"an.lu\" : 1", query);
        Assert.DoesNotContain("\"an.LivesUsed\" : 1", query);
        Assert.DoesNotContain("\"an.IsIndoor\" : 1", query);
    }

    private static IRequestExecutor CreateSchema(
        MongoResource mongoResource,
        params Fauna[] faunas)
    {
        var collection = mongoResource.CreateCollection<Fauna>("data_" + Guid.NewGuid().ToString("N"));
        collection.InsertMany(faunas);

        return new ServiceCollection()
            .AddGraphQL()
            .AddMongoDbProjections()
            .AddObjectIdConverters()
            .AddMongoDbFiltering()
            .AddMongoDbSorting()
            .AddMongoDbPagingProviders()
            .AddType(new InterfaceType<Animal>())
            .AddType(new ObjectType<Dog>(x => x.Implements<InterfaceType<Animal>>()))
            .AddType(new ObjectType<Cat>(x => x.Implements<InterfaceType<Animal>>()))
            .AddType(new ObjectType<Bird>(x => x.Implements<InterfaceType<Animal>>()))
            .AddType(
                new ObjectType<Fauna>(
                    x => x
                        .Field(f => f.Animals)
                        .Type<ListType<NonNullType<InterfaceType<Animal>>>>()))
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("faunas")
                    .Type<ListType<NonNullType<ObjectType<Fauna>>>>()
                    .Resolve(_ => collection.AsExecutable())
                    .Use(
                        next => async context =>
                        {
                            await next(context);

                            if (context.Result is IExecutable executable)
                            {
                                context.ContextData["query"] = executable.Print();
                            }
                        })
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .UseRequest(
                (_, next) => async context =>
                {
                    await next(context);

                    if (context.ContextData.TryGetValue("query", out var queryString))
                    {
                        var operationResult = context.Result.ExpectOperationResult();
                        operationResult.ContextData = operationResult.ContextData.SetItem("query", queryString);
                    }
                })
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync()
            .GetAwaiter()
            .GetResult();
    }

    private static void EnsureMongoMappings()
    {
        if (Interlocked.Exchange(ref s_mongoMappingsInitialized, 1) == 1)
        {
            return;
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Animal)))
        {
            BsonClassMap.RegisterClassMap<Animal>(
                x =>
                {
                    x.AutoMap();
                    x.SetIsRootClass(true);
                    x.SetDiscriminatorIsRequired(true);
                    x.AddKnownType(typeof(Dog));
                    x.AddKnownType(typeof(Cat));
                    x.AddKnownType(typeof(Bird));
                });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Dog)))
        {
            BsonClassMap.RegisterClassMap<Dog>(x => x.AutoMap());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Cat)))
        {
            BsonClassMap.RegisterClassMap<Cat>(x => x.AutoMap());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Bird)))
        {
            BsonClassMap.RegisterClassMap<Bird>(x => x.AutoMap());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Fauna)))
        {
            BsonClassMap.RegisterClassMap<Fauna>(x => x.AutoMap());
        }
    }

    public class Fauna
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("n")]
        public string Name { get; set; } = default!;

        [BsonElement("an")]
        public List<Animal> Animals { get; set; } = [];
    }

    [InterfaceType]
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(Dog), typeof(Cat), typeof(Bird))]
    public abstract class Animal
    {
        [BsonElement("n")]
        public string Name { get; set; } = default!;

        [BsonElement("age")]
        public int Age { get; set; }

        [BsonElement("spec")]
        public string Species { get; set; } = default!;
    }

    public sealed class Dog : Animal
    {
        [BsonElement("br")]
        public string Breed { get; set; } = default!;

        [BsonElement("gb")]
        public bool IsGoodBoy { get; set; }
    }

    public sealed class Cat : Animal
    {
        [BsonElement("i")]
        public bool IsIndoor { get; set; }

        [BsonElement("lu")]
        public int LivesUsed { get; set; }
    }

    public sealed class Bird : Animal
    {
        [BsonElement("cf")]
        public bool CanFly { get; set; }

        [BsonElement("ws")]
        public int WingspanCm { get; set; }
    }
}
