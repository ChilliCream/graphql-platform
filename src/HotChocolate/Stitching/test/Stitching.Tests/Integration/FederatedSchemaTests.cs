using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;
using System.Collections.Generic;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Language;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.Stitching.Integration
{
    public class FederatedSchemaTests : IClassFixture<StitchingTestContext>
    {
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
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
        }

        public TestServer CreateCustomerService() =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddCustomerSchema()
                    .PublishSchemaDefinition(c => c
                        .SetName(Context.CustomerSchema)
                        .AddTypeExtensionsFromString(
                            @"extend type Query {
                                consultant: Consultant
                                    @delegate(
                                        path: ""customer(id:\""Q3VzdG9tZXIKZDE=\"").consultant"")
                            }")),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public TestServer CreateContractService() =>
            Context.ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddContractSchema()
                    .PublishSchemaDefinition(c => c
                        .SetName(Context.ContractSchema)),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public IHttpClientFactory CreateDefaultRemoteSchemas()
        {
            var connections = new Dictionary<string, HttpClient>
            {
                { Context.CustomerSchema, CreateCustomerService().CreateClient() },
                { Context.ContractSchema, CreateContractService().CreateClient() }
            };

            return StitchingTestContext.CreateRemoteSchemas(connections);
        }
    }
}
