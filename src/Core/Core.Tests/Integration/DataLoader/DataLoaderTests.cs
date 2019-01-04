using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using GreenDonut;
using HotChocolate.DataLoader;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using HotChocolate.Utilities;
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
                        Func<Task<string>> dataLoader =
                            ctx.DataLoader<string>(
                                "fetchItems",
                                () => Task.FromResult("fooBar"));
                        return await dataLoader();
                    }).To("Query", "fetchItem");
                });

            IQueryExecuter executer = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executer.ExecuteAsync(
                new QueryRequest("{ fetchItem }")
                {
                    Services = scope.ServiceProvider
                }); ;

            // assert
            result.Snapshot();
        }

        [Fact]
        public async Task FetchOnceDataLoaderWithFactory()
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
                        Func<Task<string>> dataLoader =
                            ctx.DataLoader<string>(
                                "fetchItems",
                                services => () => Task.FromResult("fooBar"));
                        return await dataLoader();
                    }).To("Query", "fetchItem");
                });

            IQueryExecuter executer = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executer.ExecuteAsync(
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
                            ctx.DataLoader<string, string>(
                                "fetchItems",
                                key => Task.FromResult(key));
                        return await dataLoader.LoadAsync("fooBar");
                    }).To("Query", "fetchItem");
                });

            IQueryExecuter executer = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executer.ExecuteAsync(
                new QueryRequest("{ fetchItem }")
                {
                    Services = scope.ServiceProvider
                }); ;

            // assert
            result.Snapshot();
        }

        [Fact]
        public async Task FetchSingleDataLoaderWithFactory()
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
                            ctx.DataLoader<string, string>(
                                "fetchItems",
                                services => key => Task.FromResult(key));
                        return await dataLoader.LoadAsync("fooBar");
                    }).To("Query", "fetchItem");
                });

            IQueryExecuter executer = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executer.ExecuteAsync(
                new QueryRequest("{ fetchItem }")
                {
                    Services = scope.ServiceProvider
                }); ;

            // assert
            result.Snapshot();
        }

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
                            ctx.DataLoader<string, string>(
                                "fetchItems",
                                keys =>
                                Task.FromResult<IReadOnlyDictionary<string, string>>(
                                    keys.ToDictionary(t => t)));
                        return await dataLoader.LoadAsync("fooBar");
                    }).To("Query", "fetchItem");
                });

            IQueryExecuter executer = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executer.ExecuteAsync(
                new QueryRequest("{ fetchItem }")
                {
                    Services = scope.ServiceProvider
                }); ;

            // assert
            result.Snapshot();
        }

        [Fact]
        public async Task FetchDataLoaderWithFactory()
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
                            ctx.DataLoader<string, string>(
                                "fetchItems",
                                services =>
                                keys =>
                                Task.FromResult<IReadOnlyDictionary<string, string>>(
                                    keys.ToDictionary(t => t)));
                        return await dataLoader.LoadAsync("fooBar");
                    }).To("Query", "fetchItem");
                });

            IQueryExecuter executer = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executer.ExecuteAsync(
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
                            ctx.DataLoader<string, string>(
                                "fetchItems",
                                keys =>
                                Task.FromResult(keys.ToLookup(t => t)));
                        return await dataLoader.LoadAsync("fooBar");
                    }).To("Query", "fetchItem");
                });

            IQueryExecuter executer = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executer.ExecuteAsync(
                new QueryRequest("{ fetchItem }")
                {
                    Services = scope.ServiceProvider
                }); ;

            // assert
            result.Snapshot();
        }

        [Fact]
        public async Task FetchGroupDataLoaderWithFactory()
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
                            ctx.DataLoader<string, string>(
                                "fetchItems",
                                services =>
                                keys =>
                                Task.FromResult(keys.ToLookup(t => t)));
                        return await dataLoader.LoadAsync("fooBar");
                    }).To("Query", "fetchItem");
                });

            IQueryExecuter executer = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            IExecutionResult result = await executer.ExecuteAsync(
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

            var schema = Schema.Create(c =>
            {
                c.RegisterQueryType<Query>();
                c.Options.DeveloperMode = true;
            });

            IQueryExecuter executer = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader(key: ""a"")
                    b: withDataLoader(key: ""b"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader(key: ""a"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    c: withDataLoader(key: ""c"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executer.ExecuteAsync(new QueryRequest(
                "{ loads }")
            {
                Services = scope.ServiceProvider
            }));

            // assert
            Assert.Collection(results,
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors));
            results.Snapshot();
        }

        [Fact]
        public async Task ClassDataLoaderWithKey()
        {
            // arrange
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddDataLoaderRegistry()
                .BuildServiceProvider();

            var schema = Schema.Create(c =>
            {
                c.RegisterQueryType<Query>();
                c.Options.DeveloperMode = true;
            });

            IQueryExecuter executer = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader2(key: ""a"")
                    b: withDataLoader2(key: ""b"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader2(key: ""a"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    c: withDataLoader2(key: ""c"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executer.ExecuteAsync(new QueryRequest(
                "{ loads loads2 }")
            {
                Services = scope.ServiceProvider
            }));

            // assert
            Assert.Collection(results,
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors));
            results.Snapshot();
        }

        [Fact]
        public async Task StackedDataLoader()
        {
            // arrange
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddDataLoaderRegistry()
                .BuildServiceProvider();

            var schema = Schema.Create(c =>
            {
                c.RegisterQueryType<Query>();
                c.Options.DeveloperMode = true;
            });

            IQueryExecuter executer = schema.MakeExecutable();
            IServiceScope scope = serviceProvider.CreateScope();

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withStackedDataLoader(key: ""a"")
                    b: withStackedDataLoader(key: ""b"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withStackedDataLoader(key: ""a"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    c: withStackedDataLoader(key: ""c"")
                }")
            {
                Services = scope.ServiceProvider
            }));

            // assert
            Assert.Collection(results,
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors));
            results.Snapshot();
        }
    }
}
