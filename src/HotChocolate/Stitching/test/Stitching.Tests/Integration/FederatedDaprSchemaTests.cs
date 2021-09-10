using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.Execution.Caching;
using HotChocolate.Language;
using HotChocolate.Stitching.Schemas.Accounts;
using HotChocolate.Stitching.Schemas.Inventory;
using HotChocolate.Stitching.Schemas.Products;
using HotChocolate.Stitching.Schemas.Reviews;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;
using Moq;
using static HotChocolate.Tests.TestHelper;
using Dapr.Client;
using HotChocolate.Stitching.DAPR;
using System.Linq;

namespace HotChocolate.Stitching.Integration
{
    public class FederatedDaprSchemaTests
        : IClassFixture<StitchingTestContext>
    {
        private const string _accounts = "accounts";
        private const string _inventory = "inventory";
        private const string _products = "products";
        private const string _reviews = "reviews";

        private Dictionary<string, SchemaDefinitionDto> mockStateValues = new Dictionary<string, SchemaDefinitionDto>();
        private Dictionary<string, SchemaDefinitionDto> mockQueueValues = new Dictionary<string, SchemaDefinitionDto>();

        DaprClient daprClient;
        
        public FederatedDaprSchemaTests(StitchingTestContext context)
        {
            Context = context;
            var daprClientMock = new Mock<DaprClient>();

            daprClientMock.Setup(_ => _.SaveStateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SchemaDefinitionDto>(), null, null, default))
                    .Callback((string storeName, string key, SchemaDefinitionDto value, StateOptions stateOptions, IReadOnlyDictionary<string, string> meta, CancellationToken cancellationToken) =>
                    {
                        mockStateValues.TryAdd(key, value);
                    });

            daprClientMock.Setup(_ => _.GetStateAsync<SchemaDefinitionDto>(It.IsAny<string>(), It.IsAny<string>(), null, null, default))
                    .Returns((string storeName, string key, ConsistencyMode consistencyMode, IReadOnlyDictionary<string, string> dictionary, CancellationToken cancellationToken) =>
                    {
                        return new Task<SchemaDefinitionDto>(() => mockStateValues[key]);
                    });

            daprClientMock.Setup(_ => _.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SchemaDefinitionDto>(), new Dictionary<string, string>(), default))
                    .Callback((string storeName, string key, SchemaDefinitionDto value, Dictionary<string, string> data, CancellationToken cancellationToken) => 
                     {
                         mockQueueValues.TryAdd(key, value);
                     });

            daprClient = daprClientMock.Object;
        }

        private StitchingTestContext Context { get; }

        [Fact]
        public async Task AutoMerge_Schema()
        {
            // arrange
            using var cts = new CancellationTokenSource(20_000);
            NameString configurationName = "C" + Guid.NewGuid().ToString("N");
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas(configurationName);

            // act
            ISchema schema =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query"))
                    .AddRemoteSchemasFromDAPR(configurationName, daprClient)
                    .ModifyOptions(o => o.SortFieldsByName = true)
                    .BuildSchemaAsync(cancellationToken: cts.Token);

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_HotReload_Schema()
        {
            // arrange
            using var cts = new CancellationTokenSource(20_000);
            NameString configurationName = "C" + Guid.NewGuid().ToString("N");
            var schemaDefinitionV2 = FileResource.Open("AccountSchemaDefinition.json");
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas(configurationName);

            IRequestExecutorResolver executorResolver =
                new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query"))
                    .AddRemoteSchemasFromDAPR(configurationName, daprClient)
                    .Services
                    .BuildServiceProvider()
                    .GetRequiredService<IRequestExecutorResolver>();

            await executorResolver.GetRequestExecutorAsync(cancellationToken: cts.Token);
            var raised = false;

            executorResolver.RequestExecutorEvicted += (_, args) =>
            {
                if (args.Name.Equals(Schema.DefaultName))
                {
                    raised = true;
                }
            };

            // act
            Assert.False(raised, "eviction was raised before act.");
            await daprClient.SaveStateAsync(DaprConfiguration.StateStoreComponent, $"{configurationName}.{_accounts}", schemaDefinitionV2);

            //await database.StringSetAsync($"{configurationName}.{_accounts}", schemaDefinitionV2);
            //await _connection.GetSubscriber().PublishAsync(configurationName.Value, _accounts);

            // assert
            Assert.True(raised, "schema evicted.");
            IRequestExecutor executor =
                await executorResolver.GetRequestExecutorAsync(cancellationToken: cts.Token);
            ObjectType type = executor.Schema.GetType<ObjectType>("User");
            Assert.True(type.Fields.ContainsField("foo"), "foo field exists.");
        }

        [Fact]
        public async Task AutoMerge_HotReload_ClearOperationCaches()
        {
            // arrange
            using var cts = new CancellationTokenSource(20_000);
            NameString configurationName = "C" + Guid.NewGuid().ToString("N");
            var schemaDefinitionV2 = FileResource.Open("AccountSchemaDefinition.json");
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas(configurationName);
            DocumentNode document = Utf8GraphQLParser.Parse("{ foo }");
            var queryHash = "abc";

            ServiceProvider serviceProvider =
                new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query").Field("foo").Resolve("foo"))
                    .AddRemoteSchemasFromDAPR(configurationName, daprClient)
                    .Services
                    .BuildServiceProvider();

            IRequestExecutorResolver executorResolver =
                serviceProvider.GetRequiredService<IRequestExecutorResolver>();
            IDocumentCache documentCache =
                serviceProvider.GetRequiredService<IDocumentCache>();
            IPreparedOperationCache preparedOperationCache =
                serviceProvider.GetRequiredService<IPreparedOperationCache>();

            await executorResolver.GetRequestExecutorAsync(cancellationToken: cts.Token);
            var raised = false;

            executorResolver.RequestExecutorEvicted += (_, args) =>
            {
                if (args.Name.Equals(Schema.DefaultName))
                {
                    raised = true;
                }
            };

            Assert.False(documentCache.TryGetDocument(queryHash, out _));
            Assert.False(preparedOperationCache.TryGetOperation(queryHash, out _));

            IRequestExecutor requestExecutor =
                await executorResolver.GetRequestExecutorAsync(cancellationToken: cts.Token);

            await requestExecutor
                .ExecuteAsync(QueryRequestBuilder
                    .New()
                    .SetQuery(document)
                    .SetQueryHash(queryHash)
                    .Create(),
                    cts.Token);

            Assert.True(preparedOperationCache.TryGetOperation("_Default-1-abc", out _));

            // act
            //await database.StringSetAsync($"{configurationName}.{_accounts}", schemaDefinitionV2);
            //await _connection.GetSubscriber().PublishAsync(configurationName.Value, _accounts);


            // assert
            Assert.True(documentCache.TryGetDocument(queryHash, out _));
            Assert.False(preparedOperationCache.TryGetOperation(queryHash, out _));
        }

        [Fact]
        public async Task AutoMerge_Execute()
        {
            // arrange
            using var cts = new CancellationTokenSource(20_000);
            NameString configurationName = "C" + Guid.NewGuid().ToString("N");
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas(configurationName);

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query"))
                    .AddRemoteSchemasFromDAPR(configurationName, daprClient)
                    .BuildRequestExecutorAsync(cancellationToken: cts.Token);

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
                }",
                cts.Token);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact(Skip = "Test is flaky")]
        public async Task AutoMerge_AddLocal_Field_Execute()
        {
            await TryTest(async ct =>
            {
                // arrange
                NameString configurationName = "C" + Guid.NewGuid().ToString("N");
                IHttpClientFactory httpClientFactory =
                    CreateDefaultRemoteSchemas(configurationName);

                IRequestExecutor executor =
                    await new ServiceCollection()
                        .AddSingleton(httpClientFactory)
                        .AddGraphQL(configurationName)
                        .AddQueryType(d => d.Name("Query").Field("local").Resolve("I am local."))
                        .AddRemoteSchemasFromDAPR(configurationName, daprClient)
                        .BuildRequestExecutorAsync(configurationName, ct);

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
                }",
                ct);

                // assert
                result.ToJson().MatchSnapshot();
            });
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
                        .PublishToDAPR(configurationName, daprClient)),
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
                        .PublishToDAPR(configurationName, daprClient)),
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
                        .PublishToDAPR(configurationName, daprClient)),
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
                        .PublishToDAPR(configurationName, daprClient)),
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
