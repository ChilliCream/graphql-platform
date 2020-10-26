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

namespace HotChocolate.Stitching.Integration
{
    public class FederatedSchemaTests : IClassFixture<StitchingTestContext>
    {
        public const string Accounts = "accounts";
        public const string Inventory = "inventory";
        public const string Products = "products";
        public const string Reviews = "reviews";

        public FederatedSchemaTests(StitchingTestContext context)
        {
            Context = context;
        }

        protected StitchingTestContext Context { get; }

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
                    .AddRemoteSchema(Accounts)
                    .AddRemoteSchema(Inventory)
                    .AddRemoteSchema(Products)
                    .AddRemoteSchema(Reviews)
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
                    .AddRemoteSchema(Accounts)
                    .AddRemoteSchema(Inventory)
                    .AddRemoteSchema(Products)
                    .AddRemoteSchema(Reviews)
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
                    .AddRemoteSchema(Accounts)
                    .AddRemoteSchema(Inventory)
                    .AddRemoteSchema(Products)
                    .AddRemoteSchema(Reviews)
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

        public TestServer CreateAccountsService() =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddAccountsSchema(),
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
                    .AddInventorySchema(),
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
                    .AddProductsSchema(),
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
                    .AddReviewSchema(),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public IHttpClientFactory CreateDefaultRemoteSchemas()
        {
            var connections = new Dictionary<string, HttpClient>
            {
                { Accounts, CreateAccountsService().CreateClient() },
                { Inventory, CreateInventoryService().CreateClient() },
                { Products, CreateProductsService().CreateClient() },
                { Reviews, CreateReviewsService().CreateClient() },
            };

            return StitchingTestContext.CreateRemoteSchemas(connections);
        }
    }
}
