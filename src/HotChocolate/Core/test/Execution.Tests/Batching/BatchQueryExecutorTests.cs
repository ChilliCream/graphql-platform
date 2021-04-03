using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using HotChocolate.Tests;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;
using HotChocolate.Resolvers;
using System.Threading;
using System.Linq;

namespace HotChocolate.Execution.Batching
{
    public class BatchQueryExecutorTests
    {
        [Fact]
        public async Task ExecuteExportScalar()
        {
            // arrange
            Snapshot.FullName();

            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddStarWarsTypes()
                .AddExportDirectiveType()
                .Services
                .AddStarWarsRepositories());

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

            IBatchQueryResult batchResult = await executor.ExecuteBatchAsync(batch);

            // assert
            await batchResult.ToJsonAsync().MatchSnapshotAsync();
        }


        [Fact]
        public async Task ExecuteExportObject()
        {
            // arrange
            Snapshot.FullName();

            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddStarWarsTypes()
                .AddExportDirectiveType()
                .AddInMemorySubscriptions()
                .Services
                .AddStarWarsRepositories());

            // act
            var batch = new List<IReadOnlyQueryRequest>
            {
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        mutation firstReview {
                            createReview(
                                episode: NEW_HOPE
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

            IBatchQueryResult batchResult = await executor.ExecuteBatchAsync(batch);

            // assert
            await batchResult.ToJsonAsync().MatchSnapshotAsync();
        }

        [Fact]
        public async Task ExecuteExportLeafList()
        {
            // arrange
            Snapshot.FullName();

            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddQueryType(d => d.Name("Query")
                    .Field("foo")
                    .Argument("bar", a => a.Type<ListType<StringType>>())
                    .Type<ListType<StringType>>()
                    .Resolver<List<string>>(c =>
                    {
                        var list = c.ArgumentValue<List<string>>("bar");
                        if (list is null)
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
                .AddExportDirectiveType());

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

            IBatchQueryResult batchResult = await executor.ExecuteBatchAsync(batch);

            // assert
            await batchResult.ToJsonAsync().MatchSnapshotAsync();
        }

        [Fact]
        public async Task ExecuteExportObjectList()
        {
            // arrange
            Snapshot.FullName();

            IRequestExecutor executor = await CreateExecutorAsync(c => c
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
                    }")
                .AddResolver("Query", "foo", c =>
                {
                    var list = c.ArgumentValue<List<object>>("f");
                    if (list is null)
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
                .UseField(next => context =>
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
                .AddExportDirectiveType());


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

            IBatchQueryResult batchResult = await executor.ExecuteBatchAsync(batch);

            // assert
            await batchResult.ToJsonAsync().MatchSnapshotAsync();
        }

        [Fact]
        public async Task Add_Value_To_Variable_List()
        {
            // arrange
            Snapshot.FullName();

            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddQueryType(d => d.Name("Query")
                    .Field("foo")
                    .Argument("bar", a => a.Type<ListType<StringType>>())
                    .Type<ListType<StringType>>()
                    .Resolver(c =>
                    {
                        List<string> list = c.ArgumentValue<List<string>>("bar");
                        list.Add("789");
                        return list;
                    }))
                .AddExportDirectiveType());

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

            IBatchQueryResult batchResult = await executor.ExecuteBatchAsync(batch);

            // assert
            await batchResult.ToJsonAsync().MatchSnapshotAsync();
        }

        [Fact]
        public async Task Convert_List_To_Single_Value_With_Converters()
        {
            // arrange
            Snapshot.FullName();

            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddQueryType(d =>
                {
                    d.Name("Query");

                    d.Field("foo")
                        .Argument("bar", a => a.Type<ListType<StringType>>())
                        .Type<ListType<StringType>>()
                        .Resolver<List<string>>(c =>
                        {
                            var list = c.ArgumentValue<List<string>>("bar");
                            list.Add("789");
                            return list;
                        });

                    d.Field("baz")
                        .Argument("bar", a => a.Type<StringType>())
                        .Resolver(c => c.ArgumentValue<string>("bar"));
                })
                .AddExportDirectiveType());

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

            IBatchQueryResult batchResult = await executor.ExecuteBatchAsync(batch);

            // assert
            await batchResult.ToJsonAsync().MatchSnapshotAsync();
        }

        [Fact]
        public async Task Batch_Is_Null()
        {
           // arrange
            Snapshot.FullName();

            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddStarWarsTypes()
                .AddExportDirectiveType()
                .Services
                .AddStarWarsRepositories());

            // act
            Func<Task> action = () => executor.ExecuteBatchAsync(null);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        // shared code for AllowParallel_Basic_... tests
        private async Task AllowParallel_Basic(bool allowParallelExecution, bool batchScoped)
        {
            // arrange
            Snapshot.FullName();

            int batchCount = 0;
            var services = new ServiceCollection();
            services.AddGraphQL()
                .AddQueryType(d => d.Name("Query")
                    .Field("foo")
                    .Argument("bar", a => a.Type<StringType>())
                    .Type<StringType>()
                    .Resolve(async c =>
                    {
                        var bar = c.ArgumentValue<string>("bar");
                        return await c.BatchDataLoader<string, string>((keys, ctxToken) =>
                        {
                            Interlocked.Increment(ref batchCount);
                            return Task.FromResult(keys.ToDictionary(x => x, x => $"{x}-{batchCount}") as IReadOnlyDictionary<string, string>);
                        }, "foo").LoadAsync(bar, CancellationToken.None);
                    }))
                .AddExportDirectiveType();

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            IServiceScope scope = null;
            if (batchScoped)
            {
                scope = serviceProvider.CreateScope();
                serviceProvider = scope.ServiceProvider;
            }
            try
            {
                IRequestExecutor executor = await serviceProvider.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();
                var requestServices = scope is not null ? serviceProvider : null;

                // act
                var batch = new List<IReadOnlyQueryRequest>
                {
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{ f1: foo(bar:""A""), f2: foo(bar:""B"") }")
                        .TrySetServices(requestServices)
                        .Create(),
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{ f3: foo(bar:""C"") @export(as: ""var"") }")
                        .TrySetServices(requestServices)
                        .Create(),
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{ f4: foo(bar:$var) }")
                        .TrySetServices(requestServices)
                        .Create()
                };

                IBatchQueryResult batchResult = await executor.ExecuteBatchAsync(batch, allowParallelExecution);

                // assert
                await batchResult.ToJsonAsync().MatchSnapshotAsync();
            }
            finally
            {
                scope?.Dispose();
            }
        }

        [Fact]
        public Task AllowParallel_Basic_Off()
        {
            return AllowParallel_Basic(false, false);
        }

        [Fact]
        public Task AllowParallel_Basic_On()
        {
            return AllowParallel_Basic(true, true);
        }

        [Fact]
        public Task AllowParallel_Basic_OnButNoScope()
        {
            return AllowParallel_Basic(true, false);
        }

        [Fact]
        public async Task AllowParallel_Regression_BatchScheduler()
        {
            // arrange
            Snapshot.FullName();

            int batchCount = 0;
            var services = new ServiceCollection();
            services.AddGraphQL()
                .AddQueryType(d => d.Name("Query")
                    .Field("foo")
                    .Argument("bar", a => a.Type<StringType>())
                    .Type<StringType>()
                    .Resolve(async c =>
                    {
                        var bar = c.ArgumentValue<string>("bar");
                        var phase1 = await c.BatchDataLoader<string, string>((keys, ctxToken) =>
                        {
                            Interlocked.Increment(ref batchCount);
                            return Task.FromResult(keys.ToDictionary(x => x, x => $"{x}-{batchCount}") as IReadOnlyDictionary<string, string>);
                        }, "foo_phase1").LoadAsync(bar, CancellationToken.None);
                        return await c.BatchDataLoader<string, string>((keys, ctxToken) =>
                        {
                            Interlocked.Increment(ref batchCount);
                            return Task.FromResult(keys.ToDictionary(x => x, x => $"{x}-{batchCount}") as IReadOnlyDictionary<string, string>);
                        }, "foo_phase2").LoadAsync(phase1, CancellationToken.None);
                    }))
                .AddExportDirectiveType();

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                IRequestExecutor executor = await scope.ServiceProvider.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

                // act
                var batch = new List<IReadOnlyQueryRequest>
                {
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{ f1: foo(bar:""A""), f2: foo(bar:""B"") }")
                        .TrySetServices(scope.ServiceProvider)
                        .Create(),
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{ f3: foo(bar:""A""), f4: foo(bar:""B""), f5: foo(bar:""C"") }")
                        .TrySetServices(scope.ServiceProvider)
                        .Create()
                };

                IBatchQueryResult batchResult = await executor.ExecuteBatchAsync(batch, true);

                // assert
                await batchResult.ToJsonAsync().MatchSnapshotAsync();
            }
        }
    }
}
