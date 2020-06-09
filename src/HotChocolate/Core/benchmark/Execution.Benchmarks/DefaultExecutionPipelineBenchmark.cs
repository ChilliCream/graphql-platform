using System;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class DefaultExecutionPipelineBenchmark
    {
        private readonly IRequestExecutor _executor;
        private readonly IReadOnlyQueryRequest _getHeroRequest;
        private readonly IReadOnlyQueryRequest _getHeroWithFriendsRequest;
        private readonly IReadOnlyQueryRequest _introspectionRequest;

        public DefaultExecutionPipelineBenchmark()
        {
            var md5 = new MD5DocumentHashProvider();
            var resources = new ResourceHelper();
            var services = new ServiceCollection()
                .AddStarWarsRepositories()
                .AddGraphQL()
                .AddStarWarsTypes()
                .Services
                .BuildServiceProvider();

            _executor = services
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync()
                .Result;
            _getHeroRequest = CreateRequest(md5, resources, "GetHeroQuery.graphql");
            _getHeroWithFriendsRequest = CreateRequest(md5, resources, "GetHeroWithFriendsQuery.graphql");
            _introspectionRequest = CreateRequest(md5, resources, "IntrospectionQuery.graphql");
        }

        private static IReadOnlyQueryRequest CreateRequest(
            MD5DocumentHashProvider md5,
            ResourceHelper resources,
            string resourceName)
        {
            string query = resources.GetResourceString(resourceName);
            string hash = md5.ComputeHash(Encoding.UTF8.GetBytes(query).AsSpan());
            DocumentNode document = Utf8GraphQLParser.Parse(query);

            return QueryRequestBuilder.New()
                .SetQuery(document)
                .SetQueryHash(hash)
                .SetQueryName(hash)
                .Create();
        }

        [Benchmark]
        public Task SchemaIntrospection()
        {
            return OneRequest(_executor, _introspectionRequest);
        }

        [Benchmark]
        public Task SchemaIntrospectionFiveParallelRequests()
        {
            return FiveRequestsInParallel(_executor, _introspectionRequest);
        }

        [Benchmark]
        public Task GetHero()
        {
            return OneRequest(_executor, _getHeroRequest);
        }

        [Benchmark]
        public Task GetHeroFiveParallelRequests()
        {
            return FiveRequestsInParallel(_executor, _getHeroRequest);
        }

        [Benchmark]
        public Task GetHeroWithFriends()
        {
            return OneRequest(_executor, _getHeroWithFriendsRequest);
        }

        [Benchmark]
        public Task GetHeroWithFriendsFiveParallelRequests()
        {
            return FiveRequestsInParallel(_executor, _getHeroWithFriendsRequest);
        }

        private static async Task OneRequest(
            IRequestExecutor executer,
            IReadOnlyQueryRequest request)
        {
            IExecutionResult result = await executer.ExecuteAsync(request);
            // var jsonWriter = new HotChocolate.Execution.Serialization.JsonQueryResultSerializer(true);
            // Console.WriteLine(jsonWriter.Serialize((IReadOnlyQueryResult)result));
            ((IDisposable)result).Dispose();
        }

        private static async Task FiveRequestsInParallel(
            IRequestExecutor executer,
            IReadOnlyQueryRequest request)
        {
            Task<IExecutionResult> task1 = executer.ExecuteAsync(request);
            Task<IExecutionResult> task2 = executer.ExecuteAsync(request);
            Task<IExecutionResult> task3 = executer.ExecuteAsync(request);
            Task<IExecutionResult> task4 = executer.ExecuteAsync(request);
            Task<IExecutionResult> task5 = executer.ExecuteAsync(request);
            ((IDisposable)(await task1)).Dispose();
            ((IDisposable)(await task2)).Dispose();
            ((IDisposable)(await task3)).Dispose();
            ((IDisposable)(await task4)).Dispose();
            ((IDisposable)(await task5)).Dispose();
        }
    }
}
