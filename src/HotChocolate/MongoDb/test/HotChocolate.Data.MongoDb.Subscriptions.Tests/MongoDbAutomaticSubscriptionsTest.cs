using HotChocolate.Data.MongoDb.Extensions;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squadron;

namespace HotChocolate.Data;

public class MongoDbAutomaticSubscriptionsTest : IClassFixture<MongoReplicaSetResource>
{
    private readonly MongoReplicaSetResource _mongoResource;

    public MongoDbAutomaticSubscriptionsTest(MongoReplicaSetResource mongoResource)
    {
        _mongoResource = mongoResource;
    }

    [Fact]
    public async Task Schema_Contains_Schema()
    {
        // Arrange
        var executor = await CreateSchema();
        var request = QueryRequestBuilder
            .New()
            .SetQuery("subscription {onTestDataCreate { name}}")
            .Create();
        var collection = _mongoResource.CreateCollection<TestData>("test-data");

        // Act
        var result = await executor.ExecuteAsync(request);
        var created = result.ExpectResponseStream().ReadResultsAsync();
        await collection.InsertOneAsync(new TestData("Bla"));

        await foreach (var bla in created)
        {
            break;
        }

    }

    private async Task<IRequestExecutor> CreateSchema()
    {
        var db = _mongoResource.CreateDatabase("test");
        await db.CreateCollectionAsync("test-data");
        var collection = db.GetCollection<TestData>("test-data");

        return await new ServiceCollection()
            .AddSingleton<IMongoCollection<TestData>>(_ => collection)
            .AddGraphQL()
            .ModifyOptions(options => options.StrictValidation = false)
            .AddCreateSubscriptions<TestData>()
            .BuildRequestExecutorAsync();
    }
}

public record TestData(string Name);
