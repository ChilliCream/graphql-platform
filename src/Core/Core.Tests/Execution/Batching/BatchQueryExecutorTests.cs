using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.StarWars;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Batching
{
    public class BatchQueryExecutorTests
    {
        [Fact]
        public async Task ExecuteExportScalar()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddStarWarsRepositories()
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();

            QueryExecutionBuilder.BuildDefault(serviceCollection);

            serviceCollection.AddSingleton<ISchema>(sp =>
                SchemaBuilder.New()
                    .AddStarWarsTypes()
                    .AddDirectiveType<ExportDirectiveType>()
                    .AddServices(sp)
                    .Create());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            var executor = services.GetService<IBatchQueryExecutor>();

            // act
            var batch = new List<IReadOnlyQueryRequest>
            {
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        query getHero {
                            hero(episode: EMPIRE) {
                                id @export
                            }
                        }")
                    .Create(),
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        query getHuman {
                            human(id: $id) {
                                name
                            }
                        }")
                    .Create()
            };

            IResponseStream stream =
                await executor.ExecuteAsync(batch, CancellationToken.None);

            var results = new List<IReadOnlyQueryResult>();
            while (!stream.IsCompleted)
            {
                IReadOnlyQueryResult result = await stream.ReadAsync();
                if (result != null)
                {
                    results.Add(result);
                }
            }

            Assert.Collection(results,
                r => r.MatchSnapshot(new SnapshotNameExtension("1")),
                r => r.MatchSnapshot(new SnapshotNameExtension("2")));
        }

        [Fact]
        public async Task ExecuteExportObject()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInMemorySubscriptionProvider();

            serviceCollection
                .AddStarWarsRepositories()
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();

            QueryExecutionBuilder.BuildDefault(serviceCollection);

            serviceCollection.AddSingleton<ISchema>(sp =>
                SchemaBuilder.New()
                    .AddStarWarsTypes()
                    .AddDirectiveType<ExportDirectiveType>()
                    .AddServices(sp)
                    .Create());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            var executor = services.GetService<IBatchQueryExecutor>();

            // act
            var batch = new List<IReadOnlyQueryRequest>
            {
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        mutation firstReview {
                            createReview(
                                episode: NEWHOPE
                                review: { commentary: ""foo"", stars: 4 })
                                    @export(as: ""r"") {
                                commentary
                                stars
                            }
                        }")
                    .Create(),
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        mutation secondReview {
                            createReview(
                                episode: EMPIRE
                                review: $r) {
                                commentary
                                stars
                            }
                        }")
                    .Create()
            };

            IResponseStream stream =
                await executor.ExecuteAsync(batch, CancellationToken.None);

            var results = new List<IReadOnlyQueryResult>();
            while (!stream.IsCompleted)
            {
                IReadOnlyQueryResult result = await stream.ReadAsync();
                if (result != null)
                {
                    results.Add(result);
                }
            }

            Assert.Collection(results,
                r => r.MatchSnapshot(new SnapshotNameExtension("1")),
                r => r.MatchSnapshot(new SnapshotNameExtension("2")));
        }

        [Fact]
        public async Task ExecuteExportLeafList()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddSingleton<ISchema>(sp => SchemaBuilder.New()
                    .AddServices(sp)
                    .AddDirectiveType<ExportDirectiveType>()
                    .AddQueryType(d => d.Name("Query")
                        .Field("foo")
                        .Argument("bar", a => a.Type<ListType<StringType>>())
                        .Type<ListType<StringType>>()
                        .Resolver<List<string>>(c =>
                        {
                            var list = c.Argument<List<string>>("bar");
                            if (list == null)
                            {
                                return new List<string>
                                {
                                    "123",
                                    "456"
                                };
                            }
                            else
                            {
                                list.Add("789");
                                return list;
                            }
                        }))
                    .Create())
                    .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();

            QueryExecutionBuilder.BuildDefault(serviceCollection);

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            var executor = services.GetService<IBatchQueryExecutor>();

            // act
            var batch = new List<IReadOnlyQueryRequest>
            {
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            foo @export(as: ""b"")
                        }")
                    .Create(),
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            foo(bar: $b)
                        }")
                    .Create()
            };

            IResponseStream stream =
                await executor.ExecuteAsync(batch, CancellationToken.None);

            var results = new List<IReadOnlyQueryResult>();
            while (!stream.IsCompleted)
            {
                IReadOnlyQueryResult result = await stream.ReadAsync();
                if (result != null)
                {
                    results.Add(result);
                }
            }

            Assert.Collection(results,
                r => r.MatchSnapshot(new SnapshotNameExtension("1")),
                r => r.MatchSnapshot(new SnapshotNameExtension("2")));
        }

        [Fact]
        public async Task ExecuteExportObjectList()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddSingleton<ISchema>(sp => SchemaBuilder.New()
                    .AddServices(sp)
                    .AddDirectiveType<ExportDirectiveType>()
                    .AddDocumentFromString(
                    @"
                    type Query {
                        foo(f: [FooInput]) : [Foo]
                    }

                    type Foo {
                        bar: String!
                    }

                    input FooInput {
                        bar: String!
                    }
                    ")
                    .AddResolver("Query", "foo", c =>
                    {
                        var list =
                            c.Argument<List<object>>("f");
                        if (list == null)
                        {
                            return new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    { "bar" , "123" }
                                }
                            };
                        }
                        else
                        {
                            list.Add(new Dictionary<string, object>
                            {
                                { "bar" , "456" }
                            });
                            return list;
                        }
                    })
                    .Use(next => context =>
                    {
                        object o = context.Parent<object>();
                        if (o is Dictionary<string, object> d
                            && d.TryGetValue(
                                context.ResponseName,
                                out object v))
                        {
                            context.Result = v;
                        }
                        return next(context);
                    })
                    .Create())
                    .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();

            QueryExecutionBuilder.BuildDefault(serviceCollection);

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            var executor = services.GetService<IBatchQueryExecutor>();

            // act
            var batch = new List<IReadOnlyQueryRequest>
            {
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            foo @export(as: ""b"")
                            {
                                bar
                            }
                        }")
                    .Create(),
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            foo(f: $b)
                            {
                                bar
                            }
                        }")
                    .Create()
            };

            IResponseStream stream =
                await executor.ExecuteAsync(batch, CancellationToken.None);

            var results = new List<IReadOnlyQueryResult>();
            while (!stream.IsCompleted)
            {
                IReadOnlyQueryResult result = await stream.ReadAsync();
                if (result != null)
                {
                    results.Add(result);
                }
            }

            Assert.Collection(results,
                r => r.MatchSnapshot(new SnapshotNameExtension("1")),
                r => r.MatchSnapshot(new SnapshotNameExtension("2")));
        }
    }
}
