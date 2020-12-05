using System;
using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Data.MongoDb.Sorting.Convention.Extensions;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Squadron;
using Xunit;

namespace HotChocolate.Data.MongoDb.Sorting
{
    public class MongoDbFindFluentTests : IClassFixture<MongoResource>
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

        private static readonly Baz[] _bazEntities =
        {
            new Baz { Bar = new DateTime(2020, 1, 12), Qux = 1 },
            new Baz { Bar = new DateTime(2020, 1, 11), Qux = 0 },
            new Baz { Bar = new DateTime(1996, 1, 11), Qux = -1 }
        };

        private readonly MongoResource _resource;

        public MongoDbFindFluentTests(MongoResource resource)
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
                    return collection.Find(FilterDefinition<Foo>.Empty).AsExecutable();
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
                    return collection.Find(FilterDefinition<Bar>.Empty).AsExecutable();
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

        [Fact]
        public async Task FindFluent_CombineQuery()
        {
            // arrange
            IRequestExecutor tester = CreateSchema(
                () =>
                {
                    IMongoCollection<Baz> collection =
                        _resource.CreateCollection<Baz>("data_" + Guid.NewGuid().ToString("N"));

                    collection.InsertMany(_bazEntities);

                    return collection
                        .Find(x => x.Bar > new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero))
                        .Sort(Builders<Baz>.Sort.Ascending(x => x.Qux))
                        .AsExecutable();
                });

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: ASC}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("ASC");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: DESC}){ bar}}")
                    .Create());

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

        public class Baz
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public DateTimeOffset Bar { get; set; }

            public int Qux { get; set; }
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
