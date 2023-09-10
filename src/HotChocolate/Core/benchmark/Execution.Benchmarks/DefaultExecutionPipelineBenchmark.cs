using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language;
using HotChocolate.StarWars;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class DefaultExecutionPipelineBenchmark
    {
        private readonly IRequestExecutor _executor;
        private readonly IQueryRequest _getHeroRequest;
        private readonly IQueryRequest _getHeroWithFriendsRequest;
        private readonly IQueryRequest _getTwoHeroesWithFriendsRequest;
        private readonly IQueryRequest _largeQuery;
        private readonly IQueryRequest _introspectionRequest;

        public DefaultExecutionPipelineBenchmark()
        {
            var md5 = new MD5DocumentHashProvider();
            var resources = new ResourceHelper();
            var services = new ServiceCollection()
                .AddStarWarsRepositories()
                .AddGraphQL()
                .AddStarWarsTypes()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            _executor = services
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync()
                .Result;
            _getHeroRequest = CreateRequest(md5, resources, "GetHeroQuery.graphql");
            _getHeroWithFriendsRequest = CreateRequest(md5, resources, "GetHeroWithFriendsQuery.graphql");
            _getTwoHeroesWithFriendsRequest = CreateRequest(md5, resources, "GetTwoHeroesWithFriendsRequest.graphql");
            _largeQuery = CreateRequest(md5, resources, "LargeQuery.graphql");
            _introspectionRequest = CreateRequest(md5, resources, "IntrospectionQuery.graphql");

            SchemaIntrospection().Wait();
        }

        private static IQueryRequest CreateRequest(
            MD5DocumentHashProvider md5,
            ResourceHelper resources,
            string resourceName)
        {
            var query = resources.GetResourceString(resourceName);
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(query).AsSpan());
            var document = Utf8GraphQLParser.Parse(query);

            return QueryRequestBuilder.New()
                .SetQuery(document)
                .SetQueryHash(hash)
                .SetQueryId(hash)
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
        // [Benchmark]
        public Task GetHero()
        {
            return OneRequest(_executor, _getHeroRequest);
        }

        // [Benchmark]
        public Task GetHeroFiveParallelRequests()
        {
            return FiveRequestsInParallel(_executor, _getHeroRequest);
        }

        // note : 2 cascading data fetches
        // [Benchmark]
        public Task GetHeroWithFriends()
        {
            return OneRequest(_executor, _getHeroWithFriendsRequest);
        }

        // [Benchmark]
        public Task GetHeroWithFriendsFiveParallelRequests()
        {
            return FiveRequestsInParallel(_executor, _getHeroWithFriendsRequest);
        }

        // note : 4 data fetches (2 parallel 2 cascading)
        // [Benchmark]
        public Task GetTwoHeroesWithFriends()
        {
            return OneRequest(_executor, _getTwoHeroesWithFriendsRequest);
        }

        // [Benchmark]
        public Task GetTwoHeroesWithFriendsFiveParallelRequests()
        {
            return FiveRequestsInParallel(_executor, _getTwoHeroesWithFriendsRequest);
        }

        // note : large query
        // [Benchmark]
        public Task LargeQuery()
        {
            return OneRequest(_executor, _largeQuery);
        }

        // [Benchmark]
        public Task LargeQueryFiveParallelRequests()
        {
            return FiveRequestsInParallel(_executor, _largeQuery);
        }

        private static async Task OneRequest(
            IRequestExecutor executor,
            IQueryRequest request)
        {
            var result = (await executor.ExecuteAsync(request)).ExpectQueryResult();

            if (result.Errors is { Count: > 0 })
            {
                Console.WriteLine("Full Error:");
                Console.WriteLine(result.ToJson());
                throw new InvalidOperationException(result.Errors[0].Message);
            }

            // var jsonWriter = new HotChocolate.Execution.Serialization.JsonQueryResultSerializer(true);
            // Console.WriteLine(jsonWriter.Serialize((IQueryResult)result));
            await result.DisposeAsync();
        }

        private static async Task FiveRequestsInParallel(
            IRequestExecutor executor,
            IQueryRequest request)
        {
            var task1 = OneRequest(executor, request);
            var task2 = OneRequest(executor, request);
            var task3 = OneRequest(executor, request);
            var task4 = OneRequest(executor, request);
            var task5 = OneRequest(executor, request);

            await WaitForTask(task1);
            await WaitForTask(task2);
            await WaitForTask(task3);
            await WaitForTask(task4);
            await WaitForTask(task5);
        }

        private static async Task WaitForTask(Task task)
        {
            if (!task.IsCompleted)
            {
                await task;
            }
        }
    }
}
