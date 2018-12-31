using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.DataLoader;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Runtime;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Integration.DataLoader
{
    public class DataLoaderTests
    {
        [Fact(Skip = "Waiting for greendout release.")]
        public async Task ClassDataLoader()
        {
            // arrange
            ISchema schema = CreateSchema(ExecutionScope.Request);
            IQueryExecutionOptionsAccessor options = CreateOptions();
            IQueryExecuter executer = QueryExecutionBuilder
                .BuildDefault(schema, options);

            // act
            List<IExecutionResult> results = new List<IExecutionResult>();
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader(key: ""a"")
                    b: withDataLoader(key: ""b"")
                }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader(key: ""a"")
                }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    c: withDataLoader(key: ""c"")
                }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                "{ loads }")));

            // assert
            Assert.Collection(results,
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors));
            results.Snapshot();
        }

        private static ISchema CreateSchema(ExecutionScope scope)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDataLoaderRegistry, DataLoaderRegistry>();

            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetRequiredService<IDataLoaderRegistry>()
                .Register<TestDataLoader>();

            return Schema.Create(c =>
            {
                c.RegisterServiceProvider(serviceProvider);
                c.RegisterQueryType<Query>();
                c.Options.DeveloperMode = true;
            });
        }

        private static IQueryExecutionOptionsAccessor CreateOptions()
        {
            return new QueryExecutionOptions
            {
                ExecutionTimeout = TimeSpan.FromSeconds(30)
            };
        }
    }
}
