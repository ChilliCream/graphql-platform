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

public class MongoDbAggregateFluentTests : IClassFixture<MongoResource>
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

    public MongoDbAggregateFluentTests(MongoResource resource)
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
                return col.Aggregate().AsExecutable();
            });

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: true}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: false}}){ bar}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task AggregateFluent_Serializer()
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
                return col.Aggregate().AsExecutable();
            });

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { baz: { eq: \"2020-01-11T00:00:00Z\" } }){ baz } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { baz: { eq: \"2020-01-12T00:00:00Z\" } }){ baz } }")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "2020-01-11")
            .AddResult(res2, "2020-01-12")
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
            .AddFiltering(x => x.AddMongoDbDefaults())
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("root")
                    .Type<ListType<ObjectType<TEntity>>>()
                    .Resolve(async _ => await new ValueTask<IExecutable<TEntity>>(resolver()))
                    .Use(
                        next => async context =>
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
