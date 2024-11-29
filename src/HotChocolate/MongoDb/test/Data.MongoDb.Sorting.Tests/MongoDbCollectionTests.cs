using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Squadron;

namespace HotChocolate.Data.MongoDb.Sorting;

public class MongoDbSortCollectionTests : IClassFixture<MongoResource>
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

    public MongoDbSortCollectionTests(MongoResource resource)
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
                var collection =
                    _resource.CreateCollection<Foo>("data_" + Guid.NewGuid().ToString("N"));

                collection.InsertMany(_fooEntities);
                return collection.AsExecutable();
            });

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { bar: ASC}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { bar: DESC}){ bar}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "ASC")
            .AddResult(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Collection_Configuration()
    {
        // arrange
        BsonClassMap.RegisterClassMap<Bar>(
            x => x.MapField(y => y.Baz)
                .SetSerializer(new DateTimeOffsetSerializer(BsonType.String))
                .SetElementName("testName"));

        var tester = CreateSchema(
            () =>
            {
                var collection =
                    _resource.CreateCollection<Bar>("data_" + Guid.NewGuid().ToString("N"));

                collection.InsertMany(_barEntities);
                return collection.AsExecutable();
            });

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { baz: ASC}){ baz}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { baz: DESC}){ baz}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "ASC")
            .AddResult(res2, "DESC")
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("renameTest")]
        public bool Bar { get; set; }
    }

    public class Bar
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTimeOffset Baz { get; set; }
    }

    private static IRequestExecutor CreateSchema<TEntity>(
        Func<IExecutable<TEntity>> resolver)
        where TEntity : class
    {
        return new ServiceCollection()
            .AddGraphQL()
            .AddSorting(x => x.AddMongoDbDefaults())
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("root")
                    .Type<ListType<ObjectType<TEntity>>>()
                    .Resolve(
                        async ctx => await new ValueTask<IExecutable<TEntity>>(resolver()))
                    .Use(
                        next => async context =>
                        {
                            await next(context);
                            if (context.Result is IExecutable executable)
                            {
                                context.ContextData["query"] = executable.Print();
                            }
                        })
                    .UseSorting<SortInputType<TEntity>>())
            .UseRequest(
                next => async context =>
                {
                    await next(context);
                    if (context.ContextData.TryGetValue("query", out var queryString))
                    {
                        context.Result =
                            OperationResultBuilder
                                .FromResult(context.Result!.ExpectOperationResult())
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
