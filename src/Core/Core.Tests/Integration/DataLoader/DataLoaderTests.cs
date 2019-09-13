using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.Integration.DataLoader
{
    public class DataLoaderTests
    {
        [Fact]
        public async Task FetchOnceDataLoader()
        {
            // arrange
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddDataLoaderRegistry()
                .BuildServiceProvider();

            var schema = Schema.Create(
                @"type Query { fetchItem: String }",
                c =>
                {
                    c.BindResolver(async ctx =>
                    {
                        return await ctx.FetchOnceAsync(
                            "fetchItems",
                            () => Task.FromResult("fooBar"));

                    }).To("Query", "fetchItem");
                });

            IQueryExecutor executor = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ fetchItem }")
                    .SetServices(scope.ServiceProvider)
                    .Create());

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task FetchSingleDataLoader()
        {
            // arrange
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddDataLoaderRegistry()
                .BuildServiceProvider();

            var schema = Schema.Create(
                @"type Query { fetchItem: String }",
                c =>
                {
                    c.BindResolver(async ctx =>
                    {
                        IDataLoader<string, string> dataLoader =
                            ctx.CacheDataLoader<string, string>(
                                "fetchItems",
                                key => Task.FromResult(key));
                        return await dataLoader.LoadAsync("fooBar");
                    }).To("Query", "fetchItem");
                });

            IQueryExecutor executor = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ fetchItem }")
                    .SetServices(scope.ServiceProvider)
                    .Create());

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task FetchDataLoader()
        {
            // arrange
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddDataLoaderRegistry()
                .BuildServiceProvider();

            var schema = Schema.Create(
                @"type Query { fetchItem: String }",
                c =>
                {
                    c.BindResolver(async ctx =>
                    {
                        IDataLoader<string, string> dataLoader =
                            ctx.BatchDataLoader<string, string>(
                                "fetchItems",
                                keys =>
                                Task.FromResult<
                                    IReadOnlyDictionary<string, string>>(
                                        keys.ToDictionary(t => t)));
                        return await dataLoader.LoadAsync("fooBar");
                    }).To("Query", "fetchItem");
                });

            IQueryExecutor executor = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ fetchItem }")
                    .SetServices(scope.ServiceProvider)
                    .Create());

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task FetchGroupDataLoader()
        {
            // arrange
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddDataLoaderRegistry()
                .BuildServiceProvider();

            var schema = Schema.Create(
                @"type Query { fetchItem: String }",
                c =>
                {
                    c.BindResolver(async ctx =>
                    {
                        IDataLoader<string, string[]> dataLoader =
                            ctx.GroupDataLoader<string, string>(
                                "fetchItems",
                                keys =>
                                Task.FromResult(keys.ToLookup(t => t)));
                        return await dataLoader.LoadAsync("fooBar");
                    }).To("Query", "fetchItem");
                });

            IQueryExecutor executor = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ fetchItem }")
                    .SetServices(scope.ServiceProvider)
                    .Create());

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ClassDataLoader()
        {
            // arrange
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddDataLoaderRegistry()
                .BuildServiceProvider();

            var schema = Schema.Create(c => c.RegisterQueryType<Query>());

            IQueryExecutor executor = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withDataLoader(key: ""a"")
                            b: withDataLoader(key: ""b"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withDataLoader(key: ""a"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            c: withDataLoader(key: ""c"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ loads }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            // assert
            Assert.Collection(results,
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors));
            results.MatchSnapshot();
        }

        [Fact]
        public async Task ClassDataLoaderWithKey()
        {
            // arrange
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddDataLoaderRegistry()
                .BuildServiceProvider();

            var schema = Schema.Create(c => c.RegisterQueryType<Query>());

            IQueryExecutor executor = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withDataLoader2(key: ""a"")
                            b: withDataLoader2(key: ""b"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withDataLoader2(key: ""a"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            c: withDataLoader2(key: ""c"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ loads loads2 }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            // assert
            Assert.Collection(results,
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors));
            results.MatchSnapshot();
        }

        [Fact]
        public async Task StackedDataLoader()
        {
            // arrange
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddDataLoaderRegistry()
                .BuildServiceProvider();

            var schema = Schema.Create(c => c.RegisterQueryType<Query>());

            IQueryExecutor executor = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withStackedDataLoader(key: ""a"")
                            b: withStackedDataLoader(key: ""b"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withStackedDataLoader(key: ""a"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            c: withStackedDataLoader(key: ""c"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            // assert
            Assert.Collection(results,
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors));
            results.MatchSnapshot();
        }

        [Fact]
        public async Task ClassDataLoader_Resolve_From_DependencyInjection()
        {
            // arrange
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddDataLoader<ITestDataLoader, TestDataLoader>()
                .BuildServiceProvider();

            var schema = Schema.Create(c => c.RegisterQueryType<Query>());

            IQueryExecutor executor = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: dataLoaderWithInterface(key: ""a"")
                            b: dataLoaderWithInterface(key: ""b"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: dataLoaderWithInterface(key: ""a"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            c: dataLoaderWithInterface(key: ""c"")
                        }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ loads loads2 loads3 }")
                    .SetServices(scope.ServiceProvider)
                    .Create()));

            // assert
            Assert.Collection(results,
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors));
            results.MatchSnapshot();
        }
    }
}
