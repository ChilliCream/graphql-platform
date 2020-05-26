using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using System.Threading.Tasks;
using System;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class DefaultExecutionPipelineBenchmark
    {
        private readonly IRequestExecutor _executor;
        private readonly string _introspectionQuery;

        public DefaultExecutionPipelineBenchmark()
        {
            var services = new ServiceCollection()
                .AddStarWarsRepositories()
                .AddGraphQL()
                .ConfigureSchema(b => b.AddStarWarsTypes())
                .Services.BuildServiceProvider();

            _executor = services
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync().Result;

            var resources = new ResourceHelper();
            _introspectionQuery = resources.GetResourceString("IntrospectionQuery.graphql");
        }

        [Benchmark]
        public async Task SchemaIntrospection()
        {
            IExecutionResult result = await _executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(_introspectionQuery)
                    .Create());
            ((IDisposable)result).Dispose();
        }
    }
}
