using System;
using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.MongoDb.Sorting.Convention.Extensions;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Squadron;
using Xunit;

namespace HotChocolate.MongoDb.Data.Sorting
{
    public class MongoDbAggregateFluentTests : IClassFixture<MongoResource>
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = true }, new Foo { Bar = false }
        };

        private static readonly Bar[] _barEntities =
        {
            new Bar { Baz = new DateTime(2020, 1, 12) },
            new Bar { Baz = new DateTime(2020, 1, 11) }
        };

        private readonly MongoResource _resource;

        public MongoDbAggregateFluentTests(MongoResource resource)
        {
            _resource = resource;
        }

        [Fact]
        public async Task BsonElement_Rename()
        {
            // arrange
            IRequestExecutor tester = CreateSchema(
                () =>
                {
                    IMongoCollection<Foo> collection =
                        _resource.CreateCollection<Foo>("data_" + Guid.NewGuid().ToString("N"));

                    collection.InsertMany(_fooEntities);
                    return collection.Aggregate().AsExecutable();
                });

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: ASC}){ bar}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: DESC}){ bar}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
        }

        [Fact]
        public async Task Collection_Configuration()
        {
            // arrange
            BsonClassMap.RegisterClassMap<Bar>(
                x => x.MapField(y => y.Baz)
                    .SetSerializer(new DateTimeOffsetSerializer(BsonType.String))
                    .SetElementName("testName"));

            IRequestExecutor tester = CreateSchema(
                () =>
                {
                    IMongoCollection<Bar> collection =
                        _resource.CreateCollection<Bar>("data_" + Guid.NewGuid().ToString("N"));

                    collection.InsertMany(_barEntities);
                    return collection.Aggregate().AsExecutable();
                });

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { baz: ASC}){ baz}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { baz: DESC}){ baz}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
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
                .AddSorting(x => x.AddMongoDbDefaults())
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("root")
                        .Type<ListType<ObjectType<TEntity>>>()
                        .Resolver(
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
                        if (context.Result is IReadOnlyQueryResult result &&
                            context.ContextData.TryGetValue("query", out object? queryString))
                        {
                            context.Result =
                                QueryResultBuilder
                                    .FromResult(result)
                                    .SetContextData("query", queryString)
                                    .Create();
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
}
