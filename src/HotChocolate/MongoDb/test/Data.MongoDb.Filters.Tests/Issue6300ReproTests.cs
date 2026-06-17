using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class Issue6300ReproTests : IClassFixture<MongoResource>
{
    private readonly MongoResource _resource;

    public Issue6300ReproTests(MongoResource resource)
    {
        _resource = resource;
    }

    [Fact]
    public async Task Empty_List_Some_Filter_Should_Not_Reach_Mongo()
    {
        // arrange
        var person = new Person
        {
            Id = Guid.NewGuid().ToString("N"),
            PetList =
            [
                new Animal { Name = "dog" }
            ]
        };

        var collection = _resource.CreateCollection<Person>("data_" + Guid.NewGuid().ToString("N"));
        collection.InsertOne(person);

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .AddFiltering(x => x.AddMongoDbDefaults())
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("userProfiles")
                    .Type<ListType<ObjectType<Person>>>()
                    .Resolve(async _ => await new ValueTask<IExecutable<Person>>(collection.AsExecutable()))
                    .UseFiltering<FilterInputType<Person>>())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            $$"""
            {
              userProfiles(
                where: {
                  id: { eq: "{{person.Id}}" }
                  and: { petList: { some: {} } }
                }) {
                id
              }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        var error = Assert.Single(operationResult.Errors!);

        Assert.NotNull(error.Exception);
        Assert.Contains(
            "could not combine expressions",
            error.Exception!.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "Command find failed",
            error.Exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    public class Person
    {
        [BsonId]
        public string Id { get; set; } = default!;

        public List<Animal> PetList { get; set; } = [];
    }

    public class Animal
    {
        public string Name { get; set; } = default!;
    }
}
