using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

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
                        return await ctx.FetchOnceAsync<string>(
                            "fetchItems",
                            () => Task.FromResult("fooBar"));

                    }).To("Query", "fetchItem");
                });

            IQueryExecutor executor = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                new QueryRequest("{ fetchItem }")
                {
                    Services = scope.ServiceProvider
                }); ;

            // assert
            result.Snapshot();
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
                new QueryRequest("{ fetchItem }")
                {
                    Services = scope.ServiceProvider
                }); ;

            // assert
            result.Snapshot();
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
                new QueryRequest("{ fetchItem }")
                {
                    Services = scope.ServiceProvider
                }); ;

            // assert
            result.Snapshot();
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
                            ctx.GroupedDataLoader<string, string>(
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
                new QueryRequest("{ fetchItem }")
                {
                    Services = scope.ServiceProvider
                }); ;

            // assert
            result.Snapshot();
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

            results.Add(await executor.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader(key: ""a"")
                    b: withDataLoader(key: ""b"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executor.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader(key: ""a"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executor.ExecuteAsync(new QueryRequest(
                @"{
                    c: withDataLoader(key: ""c"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executor.ExecuteAsync(new QueryRequest(
                "{ loads }")
            {
                Services = scope.ServiceProvider
            }));

            // assert
            Assert.Collection(results,
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors));
            results.Snapshot();
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

            results.Add(await executor.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader2(key: ""a"")
                    b: withDataLoader2(key: ""b"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executor.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader2(key: ""a"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executor.ExecuteAsync(new QueryRequest(
                @"{
                    c: withDataLoader2(key: ""c"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executor.ExecuteAsync(new QueryRequest(
                "{ loads loads2 }")
            {
                Services = scope.ServiceProvider
            }));

            // assert
            Assert.Collection(results,
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors));
            results.Snapshot();
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

            results.Add(await executor.ExecuteAsync(new QueryRequest(
                @"{
                    a: withStackedDataLoader(key: ""a"")
                    b: withStackedDataLoader(key: ""b"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executor.ExecuteAsync(new QueryRequest(
                @"{
                    a: withStackedDataLoader(key: ""a"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executor.ExecuteAsync(new QueryRequest(
                @"{
                    c: withStackedDataLoader(key: ""c"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            // assert
            Assert.Collection(results,
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors),
                t => Assert.Empty(t.Errors));
            results.Snapshot();
        }
    }
}
