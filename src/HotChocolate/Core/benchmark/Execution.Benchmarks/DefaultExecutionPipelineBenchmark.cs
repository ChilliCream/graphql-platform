using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using System.Threading.Tasks;
using System;
using HotChocolate.Language;
using System.Text;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class DefaultExecutionPipelineBenchmark
    {
        private readonly IRequestExecutor _executor;
        private readonly IReadOnlyQueryRequest _request;

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

            var md5 = new MD5DocumentHashProvider();
            var resources = new ResourceHelper();
            string introspectionQuery = resources.GetResourceString("IntrospectionQuery.graphql");
            string hash = md5.ComputeHash(Encoding.UTF8.GetBytes(introspectionQuery).AsSpan());
            DocumentNode document = Utf8GraphQLParser.Parse(introspectionQuery);
            _request = QueryRequestBuilder.New()
                .SetQuery(document)
                .SetQueryHash(hash)
                .SetQueryName(hash)
                .Create();

            SchemaIntrospection().Wait();
        }

        [Benchmark]
        public async Task SchemaIntrospection()
        {
            IExecutionResult result = await _executor.ExecuteAsync(_request);
            ((IDisposable)result).Dispose();
        }
    }
}
