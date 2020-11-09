using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using HotChocolate.Stitching.Schemas.Accounts;
using HotChocolate.Stitching.Schemas.Inventory;
using HotChocolate.Stitching.Schemas.Products;
using HotChocolate.Stitching.Schemas.Reviews;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Squadron;
using StackExchange.Redis;
using Xunit;

namespace HotChocolate.Stitching.Integration
{
    public class FederatedRedisSchemaTests
        : IClassFixture<StitchingTestContext>
        , IClassFixture<RedisResource>
    {
        private const string _accounts = "accounts";
        private const string _inventory = "inventory";
        private const string _products = "products";
        private const string _reviews = "reviews";

        private readonly ConnectionMultiplexer _connection;

        public FederatedRedisSchemaTests(StitchingTestContext context, RedisResource redisResource)
        {
            Context = context;
            _connection = redisResource.GetConnection();
        }

        private StitchingTestContext Context { get; }

        [Fact]
        public async Task AutoMerge_Schema()
        {
            // arrange
            NameString configurationName = "C" + Guid.NewGuid().ToString("N");
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas(configurationName);

            IDatabase database = _connection.GetDatabase();
            for (int i = 0; i < 10; i++)
            {
                if (await database.SetLengthAsync(configurationName.Value) == 4)
                {
                    break;
                }

                await Task.Delay(150);
            }

            // act
            ISchema schema =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query"))
                    .AddRemoteSchemasFromRedis(configurationName, s => _connection)
                    .ModifyOptions(o => o.SortFieldsByName = true)
                    .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_HotReload_Schema()
        {
            // arrange
            NameString configurationName = "C" + Guid.NewGuid().ToString("N");
            var schemaDefinitionV2 = FileResource.Open("AccountSchemaDefinition.json");
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas(configurationName);

            IDatabase database = _connection.GetDatabase();
            for (var i = 0; i < 10; i++)
            {
                if (await database.SetLengthAsync(configurationName.Value) == 4)
                {
                    break;
                }

                await Task.Delay(150);
            }

            IRequestExecutorResolver executorResolver =
                new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query"))
                    .AddRemoteSchemasFromRedis(configurationName, s => _connection)
                    .Services
                    .BuildServiceProvider()
                    .GetRequiredService<IRequestExecutorResolver>();

            await executorResolver.GetRequestExecutorAsync();
            var raised = false;

            executorResolver.RequestExecutorEvicted += (sender, args) =>
            {
                if (args.Name.Equals(Schema.DefaultName))
                {
                    raised = true;
                }
            };

            // act
            Assert.False(raised, "eviction was raised before act.");
            await database.StringSetAsync($"{configurationName}.{_accounts}", schemaDefinitionV2);
            await _connection.GetSubscriber().PublishAsync(configurationName.Value, _accounts);

            for (var i = 0; i < 10; i++)
            {
                if (raised)
                {
                    break;
                }

                await Task.Delay(150);
            }

            // assert
            Assert.True(raised, "schema evicted.");
            IRequestExecutor executor = await executorResolver.GetRequestExecutorAsync();
            ObjectType type = executor.Schema.GetType<ObjectType>("User");
            Assert.True(type.Fields.ContainsField("foo"), "foo field exists.");
        }

        [Fact]
        public async Task AutoMerge_Execute()
        {
            // arrange
            NameString configurationName = "C" + Guid.NewGuid().ToString("N");
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas(configurationName);

            IDatabase database = _connection.GetDatabase();
            for (int i = 0; i < 10; i++)
            {
                if (await database.SetLengthAsync(configurationName.Value) == 4)
                {
                    break;
                }

                await Task.Delay(150);
            }

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query"))
                    .AddRemoteSchemasFromRedis(configurationName, s => _connection)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    me {
                        id
                        name
                        reviews {
                            body
                            product {
                                upc
                            }
                        }
                    }
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_AddLocal_Field_Execute()
        {
            // arrange
            NameString configurationName = "C" + Guid.NewGuid().ToString("N");
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas(configurationName);

            IDatabase database = _connection.GetDatabase();
            for (int i = 0; i < 10; i++)
            {
                if (await database.SetLengthAsync(configurationName.Value) == 4)
                {
                    break;
                }

                await Task.Delay(150);
            }

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL(configurationName)
                    .AddQueryType(d => d.Name("Query").Field("local").Resolve("I am local."))
                    .AddRemoteSchemasFromRedis(configurationName, s => _connection)
                    .BuildRequestExecutorAsync(configurationName);

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    me {
                        id
                        name
                        reviews {
                            body
                            product {
                                upc
                            }
                        }
                    }
                    local
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        public TestServer CreateAccountsService(NameString configurationName) =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
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
                        .PublishToRedis(configurationName, sp => _connection)),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public TestServer CreateInventoryService(NameString configurationName) =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
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
                        .PublishToRedis(configurationName, sp => _connection)),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public TestServer CreateProductsService(NameString configurationName) =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
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
                        .PublishToRedis(configurationName, sp => _connection)),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public TestServer CreateReviewsService(NameString configurationName) =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
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
                        .PublishToRedis(configurationName, sp => _connection)),
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

            return StitchingTestContext.CreateRemoteSchemas(connections);
        }
    }
}
