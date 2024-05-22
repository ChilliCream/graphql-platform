using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbCollectionTests : IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = true, },
        new() { Bar = false, },
    ];

    private static readonly Bar[] _barEntities =
    [
        new() { Baz = new DateTimeOffset(2020, 1, 12, 0, 0, 0, TimeSpan.Zero), },
        new() { Baz = new DateTimeOffset(2020, 1, 11, 0, 0, 0, TimeSpan.Zero), },
    ];

    private readonly MongoResource _resource;

    public MongoDbCollectionTests(MongoResource resource)
    {
        _resource = resource;
    }

    [Fact]
    public async Task BsonElement_Rename()
    {
        // arrange
        var tester = CreateSchema(
            () =>
            {
                var col = _resource.CreateCollection<Foo>("data_" + Guid.NewGuid().ToString("N"));
                col.InsertMany(_fooEntities);
                return col.AsExecutable();
            });

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: true}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: false}}){ bar}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "true"), res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task Collection_Serializer()
    {
        // arrange
        BsonClassMap.RegisterClassMap<Bar>(
            x => x.MapField(y => y.Baz)
                .SetSerializer(new DateTimeOffsetSerializer(BsonType.String))
                .SetElementName("testName"));

        var tester = CreateSchema(
            () =>
            {
                var col = _resource.CreateCollection<Bar>("data_" + Guid.NewGuid().ToString("N"));
                col.InsertMany(_barEntities);
                return col.AsExecutable();
            });

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { baz: { eq: \"2020-01-11T00:00:00Z\"}}){ baz}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { baz: { eq: \"2020-01-12T00:00:00Z\"}}){ baz}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "2020-01-11"), res2, "2020-01-12")
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("renameTest")]
        public bool Bar { get; set; }
    }

    public class Bar
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTimeOffset Baz { get; set; }
    }

    private static IRequestExecutor CreateSchema<TEntity>(
        Func<IExecutable<TEntity>> resolver)
        where TEntity : class
    {
        return new ServiceCollection()
            .AddGraphQL()
            .AddFiltering(x => x.AddMongoDbDefaults())
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("root")
                    .Type<ListType<ObjectType<TEntity>>>()
                    .Resolve(async _ => await new ValueTask<IExecutable<TEntity>>(resolver()))
                    .Use(next => async context =>
                    {
                        await next(context);
                        if (context.Result is IExecutable executable)
                        {
                            context.ContextData["query"] = executable.Print();
                        }
                    })
                    .UseFiltering<FilterInputType<TEntity>>())
            .UseRequest(
                next => async context =>
                {
                    await next(context);
                    if (context.ContextData.TryGetValue("query", out var queryString))
                    {
                        context.Result =
                            OperationResultBuilder
                                .FromResult(context.Result!.ExpectQueryResult())
                                .SetContextData("query", queryString)
                                .Build();
                    }
                })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync()
            .GetAwaiter()
            .GetResult();
    }
}
