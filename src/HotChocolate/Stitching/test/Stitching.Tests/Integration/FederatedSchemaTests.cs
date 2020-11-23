using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using HotChocolate.Stitching.Schemas.Accounts;
using HotChocolate.Stitching.Schemas.Inventory;
using HotChocolate.Stitching.Schemas.Products;
using HotChocolate.Stitching.Schemas.Reviews;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Integration
{
    public class FederatedSchemaTests : IClassFixture<StitchingTestContext>
    {
        private const string _accounts = "accounts";
        private const string _inventory = "inventory";
        private const string _products = "products";
        private const string _reviews = "reviews";

        public FederatedSchemaTests(StitchingTestContext context)
        {
            Context = context;
        }

        private StitchingTestContext Context { get; }

        [Fact]
        public async Task AutoMerge_Schema()
        {
            // arrange
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas();

            // act
            ISchema schema =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query"))
                    .AddRemoteSchema(_accounts)
                    .AddRemoteSchema(_inventory)
                    .AddRemoteSchema(_products)
                    .AddRemoteSchema(_reviews)
                    .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute()
        {
            // arrange
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query"))
                    .AddRemoteSchema(_accounts)
                    .AddRemoteSchema(_inventory)
                    .AddRemoteSchema(_products)
                    .AddRemoteSchema(_reviews)
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
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query").Field("local").Resolve("I am local."))
                    .AddRemoteSchema(_accounts)
                    .AddRemoteSchema(_inventory)
                    .AddRemoteSchema(_products)
                    .AddRemoteSchema(_reviews)
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
                    local
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Directive_Variables_Are_Correctly_Rewritten()
        {
            // arrange
            IHttpClientFactory httpClientFactory = CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query").Field("local").Resolve("I am local."))
                    .AddRemoteSchema(_accounts)
                    .AddRemoteSchema(_inventory)
                    .AddRemoteSchema(_products)
                    .AddRemoteSchema(_reviews)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"query ($if1: Boolean! $if2: Boolean! $if3: Boolean! $if4: Boolean!) {
                    me {
                        id
                        alias1: name @include(if: $if1)
                        alias2: reviews @include(if: $if2) {
                            alias3: body @include(if: $if3)
                            alias4: product @include(if: $if4) {
                                upc
                            }
                        }
                    }
                    local
                }",
                new Dictionary<string, object>
                {
                    { "if1", true },
                    { "if2", true },
                    { "if3", true },
                    { "if4", true },
                });

            // assert
            result.ToJson().MatchSnapshot();
        }

        public TestServer CreateAccountsService() =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddAccountsSchema()
                    .PublishSchemaDefinition(c => c
                        .SetName(_accounts)
                        .IgnoreRootTypes()
                        .AddTypeExtensionsFromString(
                            @"extend type Query {
                                me: User! @delegate(path: ""user(id: 1)"")
                            }

                            extend type Review {
                                author: User @delegate(path: ""user(id: $fields:authorId)"")
                            }")),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public TestServer CreateInventoryService() =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddInventorySchema()
                    .PublishSchemaDefinition(c => c
                        .SetName(_inventory)
                        .IgnoreRootTypes()
                        .AddTypeExtensionsFromString(
                            @"extend type Product {
                                inStock: Boolean
                                    @delegate(path: ""inventoryInfo(upc: $fields:upc).isInStock"")
                                shippingEstimate: Int
                                    @delegate(path: ""shippingEstimate(price: $fields:price weight: $fields:weight)"")
                            }")),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public TestServer CreateProductsService() =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddProductsSchema()
                    .PublishSchemaDefinition(c => c
                        .SetName(_products)
                        .IgnoreRootTypes()
                        .AddTypeExtensionsFromString(
                            @"extend type Query {
                                topProducts(first: Int = 5): [Product] @delegate
                            }

                            extend type Review {
                                product: Product @delegate(path: ""product(upc: $fields:upc)"")
                            }")),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public TestServer CreateReviewsService() =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddReviewSchema()
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
                            }")),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public IHttpClientFactory CreateDefaultRemoteSchemas()
        {
            var connections = new Dictionary<string, HttpClient>
            {
                { _accounts, CreateAccountsService().CreateClient() },
                { _inventory, CreateInventoryService().CreateClient() },
                { _products, CreateProductsService().CreateClient() },
                { _reviews, CreateReviewsService().CreateClient() },
            };

            return StitchingTestContext.CreateRemoteSchemas(connections);
        }
    }
}
