using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Schemas.Accounts;
using HotChocolate.Stitching.Schemas.Inventory;
using HotChocolate.Stitching.Schemas.Products;
using HotChocolate.Stitching.Schemas.Reviews;
using HotChocolate.Types;
using Microsoft.Diagnostics.Runtime;
using Squadron;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace HotChocolate.Stitching.Integration
{
    public class FederatedHotReloadRedisSchemaTests
        : IClassFixture<StitchingTestContext>
        , IClassFixture<RedisResource>
    {
        private const string _accounts = "accounts";
        private const string _inventory = "inventory";
        private const string _products = "products";
        private const string _reviews = "reviews";

        private readonly ITestOutputHelper _outputHelper;
        private readonly ConnectionMultiplexer _connection;

        public FederatedHotReloadRedisSchemaTests(
            ITestOutputHelper outputHelper,
            StitchingTestContext context, RedisResource redisResource)
        {
            _outputHelper = outputHelper;
            Context = context;
            _connection = redisResource.GetConnection();
        }

        private StitchingTestContext Context { get; }

        [Fact]
        public async Task AutoMerge_HotReload_WithSubscriptions_EnsureCorrectDisposal()
        {
            // arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            NameString configurationName = "C" + Guid.NewGuid().ToString("N");
            var schemaDefinitionV2 = FileResource.Open("AccountSchemaDefinition.json");
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas(configurationName);
            DocumentNode document = Utf8GraphQLParser.Parse(@"
subscription OnSubscriptionTest {
    OnSubscriptionTest {
        id
        name
    }
}");
            var queryHash = "abc";

            IDatabase database = _connection.GetDatabase();
            while(!cts.IsCancellationRequested)
            {
                if (await database.SetLengthAsync(configurationName.Value) == 4)
                {
                    break;
                }

                await Task.Delay(150, cts.Token);
            }

            ServiceProvider serviceProvider =
                new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddRedisSubscriptions(_ => _connection)
                    .AddGraphQL("APIGateway")
                    .AddQueryType(d => d.Name("Query").Field("foo").Resolve("foo"))
                    .AddSubscriptionType(d => d.Name("Subscription")
                        .Field("OnSubscriptionTest")
                        .Type<ListType<ObjectType<TestModel>>>()
                        .SubscribeToTopic<TestSubscriptionModel>("Testing")
                        .Resolve(
                            _ =>
                            {
                                return new List<TestModel>
                                {
                                    new()
                                    {
                                        Id = new Guid("0C0E8C16-89C4-47DD-A0AB-49E14EE0DF8E"),
                                        Name = "Testing"
                                    }
                                };
                            }))
                    .AddRemoteSchemasFromRedis(configurationName, _ => _connection)
                    .Services
                    .BuildServiceProvider();

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                IRequestExecutorResolver executorResolver =
                    scope.ServiceProvider.GetRequiredService<IRequestExecutorResolver>();

                await executorResolver.GetRequestExecutorAsync("APIGateway", cts.Token);

                var waitForEviction = new TaskCompletionSource<object>();
                cts.Token.Register(() => waitForEviction.SetCanceled());

                executorResolver.RequestExecutorEvicted += (_, args) =>
                {
                    if (args.Name.Equals("APIGateway"))
                    {
                        waitForEviction.SetResult(new object());
                    }
                };

                IRequestExecutor requestExecutor = await executorResolver.GetRequestExecutorAsync("APIGateway", cts.Token);

                IExecutionResult result = await requestExecutor
                    .ExecuteAsync(QueryRequestBuilder
                        .New()
                        .SetQuery(document)
                        .SetQueryHash(queryHash)
                        .Create(),
                        cts.Token);

                Assert.IsType<ResponseStream>(result);
                await result.DisposeAsync();

                await database.StringSetAsync($"{configurationName}.{_accounts}", schemaDefinitionV2);
                await _connection.GetSubscriber()
                    .PublishAsync(configurationName.Value, _accounts);

                await waitForEviction.Task;
            }

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                IRequestExecutorResolver executorResolver =
                    scope.ServiceProvider.GetRequiredService<IRequestExecutorResolver>();

                IRequestExecutor _ = await executorResolver.GetRequestExecutorAsync("APIGateway", cts.Token);
            }

            IEnumerable<string> schemaNames = CollectSchemas();

            var counts = schemaNames
                .Where(x => x != Schema.DefaultName)
                .GroupBy(x => x)
                .ToDictionary(x => x.Key, x => x.Count());

            foreach (var schemaName in schemaNames)
            {
                _outputHelper.WriteLine(schemaName);
            }

            Assert.All(counts.Values,
                count =>
                {
                    Assert.Equal(1, count);
                });
        }

        private static IEnumerable<string> CollectSchemas()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

#if NETCOREAPP3_1
            int processId = Process.GetCurrentProcess().Id;
#else
            int processId = Environment.ProcessId;
#endif

            using DataTarget dataTarget = DataTarget.CreateSnapshotAndAttach(processId);
            ClrInfo runtimeInfo = dataTarget.ClrVersions[0];
            ClrRuntime runtime = runtimeInfo.CreateRuntime();

            List<string> schemaNames = new List<string>();
            foreach (ClrSegment seg in runtime.Heap.Segments)
            {
                foreach (ClrObject obj in seg.EnumerateObjects())
                {
                    // If heap corruption, continue past this object.
                    if (!obj.IsValid)
                    {
                        continue;
                    }

                    if (obj.IsFree)
                    {
                        continue;
                    }

                    ClrType objType = obj.Type;
                    if (objType.Name != "HotChocolate.Schema")
                    {
                        continue;
                    }

                    ImmutableArray<ClrInstanceField> clrInstanceFields = objType.Fields;
                    ClrInstanceField field = clrInstanceFields.First(x => x.Name == "_name");
                    ClrValueType clrValueType = field.ReadStruct(obj.Address, false);
                    string schemaName = clrValueType.ReadStringField(clrValueType.Type.Fields[0].Name);
                    if (!string.IsNullOrEmpty(schemaName))
                    {
                        schemaNames.Add(schemaName);
                    }
                }
            }

            return schemaNames;
        }

        public TestServer CreateAccountsService(NameString configurationName) =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddAccountsSchema()
                    .InitializeOnStartup()
                    .PublishSchemaDefinition(c => c
                        .SetName(_accounts)
                        .IgnoreRootTypes()
                        .AddTypeExtensionsFromString(
                            @"extend type Query {
                                me: User! @delegate(path: ""user(id: 1)"")
                            }

                            extend type Review {
                                author: User @delegate(path: ""user(id: $fields:authorId)"")
                            }")
                        .PublishToRedis(configurationName, _ => _connection)),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public TestServer CreateInventoryService(NameString configurationName) =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddInventorySchema()
                    .InitializeOnStartup()
                    .PublishSchemaDefinition(c => c
                        .SetName(_inventory)
                        .IgnoreRootTypes()
                        .AddTypeExtensionsFromString(
                            @"extend type Product {
                                inStock: Boolean
                                    @delegate(path: ""inventoryInfo(upc: $fields:upc).isInStock"")

                                shippingEstimate: Int
                                    @delegate(path: ""shippingEstimate(price: $fields:price weight: $fields:weight)"")
                            }")
                        .PublishToRedis(configurationName, _ => _connection)),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public TestServer CreateProductsService(NameString configurationName) =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddProductsSchema()
                    .InitializeOnStartup()
                    .PublishSchemaDefinition(c => c
                        .SetName(_products)
                        .IgnoreRootTypes()
                        .AddTypeExtensionsFromString(
                            @"extend type Query {
                                topProducts(first: Int = 5): [Product] @delegate
                            }

                            extend type Review {
                                product: Product @delegate(path: ""product(upc: $fields:upc)"")
                            }")
                        .PublishToRedis(configurationName, _ => _connection)),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public TestServer CreateReviewsService(NameString configurationName) =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddReviewSchema()
                    .InitializeOnStartup()
                    .PublishSchemaDefinition(c => c
                        .SetName(_reviews)
                        .IgnoreRootTypes()
                        .AddTypeExtensionsFromString(
                            @"extend type User {
                                reviews: [Review]
                                    @delegate(path:""reviewsByAuthor(authorId: $fields:id)"")
                            }

                            extend type Product {
                                reviews: [Review]
                                    @delegate(path:""reviewsByProduct(upc: $fields:upc)"")
                            }")
                        .PublishToRedis(configurationName, _ => _connection)),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public IHttpClientFactory CreateDefaultRemoteSchemas(NameString configurationName)
        {
            var connections = new Dictionary<string, HttpClient>
            {
                { _accounts, CreateAccountsService(configurationName).CreateClient() },
                { _inventory, CreateInventoryService(configurationName).CreateClient() },
                { _products, CreateProductsService(configurationName).CreateClient() },
                { _reviews, CreateReviewsService(configurationName).CreateClient() },
            };

            return StitchingTestContext.CreateHttpClientFactory(connections);
        }
    }

    public class TestSubscriptionModel
    {
        public Guid Id { get; set; }
    }

    public class TestModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
