using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFindFluentTests : IClassFixture<MongoResource>
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

    private static readonly Baz[] _bazEntities =
    [
        new() { Bar = new DateTimeOffset(2020, 1, 12, 0, 0, 0, TimeSpan.Zero), },
        new() { Bar = new DateTimeOffset(2020, 1, 11, 0, 0, 0, TimeSpan.Zero), },
        new() { Bar = new DateTimeOffset(1996, 1, 11, 0, 0, 0, TimeSpan.Zero), },
    ];

    private readonly MongoResource _resource;

    public MongoDbFindFluentTests(MongoResource resource)
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
                return col.Find(FilterDefinition<Foo>.Empty).AsExecutable();
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

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "true"), res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task FindFluent_Serializer()
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
                return col.Find(FilterDefinition<Bar>.Empty).AsExecutable();
            });

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { baz: { eq: \"2020-01-11T00:00:00Z\"}}){ baz}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { baz: { eq: \"2020-01-12T00:00:00Z\"}}){ baz}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "2020-01-11"), res2, "2020-01-12")
            .MatchAsync();
    }

    [Fact]
    public async Task FindFluent_CombineQuery()
    {
        // arrange
        var tester = CreateSchema(
            () =>
            {
                var col = _resource.CreateCollection<Baz>("data_" + Guid.NewGuid().ToString("N"));
                col.InsertMany(_bazEntities);
                return col
                    .Find(x => x.Bar > new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero))
                    .AsExecutable();
            });

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"2020-01-11T00:00:00Z\"}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"2020-01-12T00:00:00Z\"}}){ bar}}")
                .Build());

        // arrange
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

    public class Baz
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTimeOffset Bar { get; set; }
    }

    private static IRequestExecutor CreateSchema<TEntity>(
        Func<IExecutable<TEntity>> resolver)
        where TEntity : class
        => new ServiceCollection()
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
