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
        private readonly IQueryExecutor _executor;
        private readonly IReadOnlyQueryRequest _getHeroRequest;
        private readonly IReadOnlyQueryRequest _getHeroWithFriendsRequest;
        private readonly IReadOnlyQueryRequest _getTwoHerosWithFriendsRequest;
        private readonly IReadOnlyQueryRequest _largeQuery;
        private readonly IReadOnlyQueryRequest _introspectionRequest;

        public DefaultExecutionPipelineBenchmark()
        {
            var md5 = new MD5DocumentHashProvider();
            var resources = new ResourceHelper();
            var services = new ServiceCollection()
                .AddStarWarsRepositories()
                .BuildServiceProvider();

            _executor = SchemaBuilder.New()
                .AddServices(services)
                .AddStarWarsTypes()
                .Create()
                .MakeExecutable();

            _getHeroRequest = CreateRequest(md5, resources, "GetHeroQuery.graphql");
            _getHeroWithFriendsRequest = CreateRequest(md5, resources, "GetHeroWithFriendsQuery.graphql");
            _getTwoHerosWithFriendsRequest = CreateRequest(md5, resources, "GetTwoHerosWithFriendsQuery.graphql");
            _largeQuery = CreateRequest(md5, resources, "LargeQuery.graphql");
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

        // note : all sync
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

        // note : 1 data fetch
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

        // note : 2 cascading data fetches
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

        // note : 4 data fetches (2 parallel 2 cascading)
        [Benchmark]
        public Task GetTwoHerosWithFriends()
        {
            return OneRequest(_executor, _getTwoHerosWithFriendsRequest);
        }

        [Benchmark]
        public Task GetTwoHerosWithFriendsFiveParallelRequests()
        {
            return FiveRequestsInParallel(_executor, _getTwoHerosWithFriendsRequest);
        }

         // note : large query
        [Benchmark]
        public Task LargeQuery()
        {
            return OneRequest(_executor, _largeQuery);
        }

        [Benchmark]
        public Task LargeQueryFiveParallelRequests()
        {
            return FiveRequestsInParallel(_executor, _largeQuery);
        }

        private static async Task<IExecutionResult> OneRequest(
            IQueryExecutor executer,
            IReadOnlyQueryRequest request)
        {
            return await executer.ExecuteAsync(request);
        }

        private static async Task<IExecutionResult> FiveRequestsInParallel(
            IQueryExecutor executer,
            IReadOnlyQueryRequest request)
        {
            Task<IExecutionResult> task1 = OneRequest(executer, request);
            Task<IExecutionResult> task2 = OneRequest(executer, request);
            Task<IExecutionResult> task3 = OneRequest(executer, request);
            Task<IExecutionResult> task4 = OneRequest(executer, request);
            Task<IExecutionResult> task5 = OneRequest(executer, request);

            await WaitForTask(task1);
            await WaitForTask(task2);
            await WaitForTask(task3);
            await WaitForTask(task4);
            return await WaitForTask(task5);
        }

        private static async Task<IExecutionResult> WaitForTask(Task<IExecutionResult> task)
        {
            if (!task.IsCompleted)
            {
                return await task;
            }
            return task.Result;
        }
    }
}
