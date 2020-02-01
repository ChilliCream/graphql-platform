using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
                    .AddExportDirectiveType()
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
            await foreach (IReadOnlyQueryResult result in stream)
            {
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
                    .AddExportDirectiveType()
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
            await foreach (IReadOnlyQueryResult result in stream)
            {
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
                    .AddExportDirectiveType()
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
            await foreach (IReadOnlyQueryResult result in stream)
            {
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
                    .AddExportDirectiveType()
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
            await foreach (IReadOnlyQueryResult result in stream)
            {
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
        public async Task Add_Value_To_Variable_List()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddSingleton<ISchema>(sp => SchemaBuilder.New()
                    .AddServices(sp)
                    .AddExportDirectiveType()
                    .AddQueryType(d => d.Name("Query")
                        .Field("foo")
                        .Argument("bar", a => a.Type<ListType<StringType>>())
                        .Type<ListType<StringType>>()
                        .Resolver<List<string>>(c =>
                        {
                            var list = c.Argument<List<string>>("bar");
                            list.Add("789");
                            return list;
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
                        @"query foo1($b: [String]) {
                            foo(bar: $b) @export(as: ""b"")
                        }")
                    .AddVariableValue("b", new[] { "123" })
                    .Create(),
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query foo2($b: [String]) {
                            foo(bar: $b)
                        }")
                    .Create()
            };

            IResponseStream stream =
                await executor.ExecuteAsync(batch, CancellationToken.None);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream)
            {
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
        public async Task Convert_List_To_Single_Value_With_Converters()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddSingleton<ISchema>(sp => SchemaBuilder.New()
                    .AddServices(sp)
                    .AddExportDirectiveType()
                    .AddQueryType(d =>
                    {
                        d.Name("Query");

                        d.Field("foo")
                            .Argument("bar", a => a.Type<ListType<StringType>>())
                            .Type<ListType<StringType>>()
                            .Resolver<List<string>>(c =>
                            {
                                var list = c.Argument<List<string>>("bar");
                                list.Add("789");
                                return list;
                            });

                        d.Field("baz")
                            .Argument("bar", a => a.Type<StringType>())
                            .Resolver(c => c.Argument<string>("bar"));
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
                        @"query foo1($b1: [String]) {
                            foo(bar: $b1) @export(as: ""b2"")
                        }")
                    .AddVariableValue("b1", new[] { "123" })
                    .Create(),
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query foo2($b2: String) {
                            baz(bar: $b2)
                        }")
                    .Create()
            };

            IResponseStream stream =
                await executor.ExecuteAsync(batch, CancellationToken.None);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream)
            {
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
        public void Schema_Is_Correctly_Set()
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
                    .AddExportDirectiveType()
                    .AddServices(sp)
                    .Create());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            // act
            var executor = services.GetService<IBatchQueryExecutor>();

            // assert
            Assert.Equal(
                services.GetService<ISchema>(),
                executor.Schema);
        }

        [Fact]
        public async Task Batch_Is_Empty()
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
                    .AddExportDirectiveType()
                    .AddServices(sp)
                    .Create());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            var executor = services.GetService<IBatchQueryExecutor>();

            // act
            Func<Task> action = () =>
                executor.ExecuteAsync(
                    Array.Empty<IReadOnlyQueryRequest>(),
                    CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        [Fact]
        public async Task Batch_Is_Null()
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
                    .AddExportDirectiveType()
                    .AddServices(sp)
                    .Create());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            var executor = services.GetService<IBatchQueryExecutor>();

            // act
            Func<Task> action = () =>
                executor.ExecuteAsync(
                    null,
                    CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }
    }
}
