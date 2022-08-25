using CookieCrumble;
using HotChocolate.Data.MongoDb.Integration.Tests.Models;
using HotChocolate.Data.MongoDb.Integration.Tests.Schema;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squadron;
using CreateCollectionOptions = Squadron.CreateCollectionOptions;

namespace HotChocolate.Data.MongoDb.Integration.Tests;

[Collection("Database")]
public class AnnotationBasedIntegrationTests : IClassFixture<MongoResource>
{
    private readonly MongoResource _mongoDb;

    public AnnotationBasedIntegrationTests(MongoResource mongoResource)
    {
        _mongoDb = mongoResource;
    }

    [Fact]
    public async Task MoviesSchemaIntegrationTests()
    {
        // arrange
        var tester = await CreateSchema();

        // act
        var res1 = await tester.ExecuteAsync(@"{
                actors(order: [{ name : ASC }]) {
                    items {
                        name
                        actedIn(order: [{ title : ASC }]) {
                            title
                        }
                    }
                }
            }");

        await SnapshotExtensions.AddResult(
                Snapshot.Create(),
                res1)
            .MatchAsync();
    }

    private async Task<IRequestExecutor> CreateSchema()
    {
        var database = _mongoDb.CreateDatabase();
        var actorOptions = new CreateCollectionOptions
        {
            CollectionName = $"data_{Guid.NewGuid():N}"
        };

        var actorCollection = _mongoDb.CreateCollection<Actor>(database, actorOptions);
        await actorCollection.BulkWriteAsync(new[]
        {
            new InsertOneModel<Actor>(new Actor
            {
                Name = "Test",
                ActedIn = new List<Movie>
                {
                    new() { Title = "Test Movie 2", },
                    new() { Title = "Test Movie 1", }
                }
            })
        });

        return await new ServiceCollection()
            .AddSingleton(actorCollection)
            .AddGraphQL()
            .AddQueryType(d => d.Name("Query")
                .Field("actors")
                .Use(
                    next => async context =>
                    {
                        await next(context);
                        if (context.Result is IExecutable executable)
                        {
                            context.ContextData["query"] = executable.Print();
                        }
                    })
            )
            .AddType<Queries>()
            .AddSorting()
            .AddMongoDbPagingProviders()
            .AddMongoDbProjections()
            .AddMongoDbFiltering()
            .AddMongoDbSorting()
            .UseRequest(
                next => async context =>
                {
                    await next(context);
                    if (context.ContextData.TryGetValue("query", out var queryString))
                    {
                        context.Result =
                            QueryResultBuilder
                                .FromResult(context.Result!.ExpectQueryResult())
                                .SetContextData("query", queryString)
                                .Create();
                    }
                })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }
}
