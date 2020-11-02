using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
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
using HotChocolate.Language;

namespace HotChocolate.Stitching.Integration
{
    public class FederatedSchemaErrorTests : IClassFixture<StitchingTestContext>
    {
        private const string _accounts = "accounts";
        private const string _inventory = "inventory";
        private const string _products = "products";
        private const string _reviews = "reviews";

        public FederatedSchemaErrorTests(StitchingTestContext context)
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
        public async Task Execute_Error_StatusCode_On_DownStream_Request()
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
                    error
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Ok_StatusCode_With_Error_On_DownStream_Request()
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
                    a: topProducts(first: 1) {
                        upc
                        error
                    }
                    b: topProducts(first: 2) {
                        upc
                        error
                    }
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        public TestServer CreateAccountsService() =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
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
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
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
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddProductsSchema()
                    .AddTypeExtension(new ObjectTypeExtension(d =>
                    {
                        d.Name("Query")
                            .Field("error")
                            .Type(new NonNullTypeNode(new NamedTypeNode("String")))
                            .Resolve(() => throw new GraphQLException("error_message_query"));
                    }))
                    .AddTypeExtension(new ObjectTypeExtension(d =>
                    {
                        d.Name("Product")
                            .Field("error")
                            .Type(new NamedTypeNode("String"))
                            .Resolve(ctx => throw new GraphQLException(
                                ErrorBuilder.New()
                                    .SetMessage("error_message_product")
                                    .SetPath(ctx.Path)
                                    .Build()));
                    }))
                    .PublishSchemaDefinition(c => c
                        .SetName(_products)
                        .IgnoreRootTypes()
                        .AddTypeExtensionsFromString(
                            @"extend type Query {
                                topProducts(first: Int = 5): [Product] @delegate
                                auth: String! @delegate
                                error: String! @delegate
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
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
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
