using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Integration
{
    public class BaseTests : IClassFixture<StitchingTestContext>
    {
        public BaseTests(StitchingTestContext context)
        {
            Context = context;
        }

        protected StitchingTestContext Context { get; }

        [Fact]
        public async Task AutoMerge_Schema()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

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

        [Fact]
        public async Task AutoMerge_Execute()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    allCustomers {
                        id
                        name
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_Inline_Fragment()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Customer {
                            contracts: [Contract!]
                                @delegate(
                                    schema: ""contract"",
                                    path: ""contracts(customerId:$fields:id)"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    customer(id: ""Q3VzdG9tZXIKZDE="") {
                        name
                        consultant {
                            name
                        }
                        contracts {
                            id
                            ... on LifeInsuranceContract {
                                premium
                            }
                            ... on SomeOtherContract {
                                expiryDate
                            }
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_Fragment_Definition()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Customer {
                            contracts: [Contract!]
                                @delegate(
                                    schema: ""contract"",
                                    path: ""contracts(customerId:$fields:id)"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    customer(id: ""Q3VzdG9tZXIKZDE="") {
                        name
                        consultant {
                          name
                        }
                        contracts {
                            id
                            ...a
                            ...b
                        }
                    }
                }

                fragment a on LifeInsuranceContract {
                    premium
                }

                fragment b on SomeOtherContract {
                    expiryDate
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_Variables()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Customer {
                            contracts: [Contract!]
                                @delegate(
                                    schema: ""contract"",
                                    path: ""contracts(customerId:$fields:id)"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            var variables = new Dictionary<string, object>
            {
                { "customerId", "Q3VzdG9tZXIKZDE=" },
                { "deep", "deep" },
                { "deeper", "deeper" }
            };

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"query customer_query(
                    $customerId: ID!
                    $deep: String!
                    $deeper: String!
                    $deeperArray: String
                    $complex: ComplexInputType
                    $deeperInArray: String
                ) {
                    customer(id: $customerId) {
                        name
                        consultant {
                            name
                        }
                        complexArg(
                            arg: {
                                value: $deep
                                deeper: {
                                    value: ""CONSTANT""
                                    deeper: {
                                        value: $deeper
                                        deeperArray: [
                                            {
                                                value: ""CONSTANT_ARRAY"",
                                                deeper: {
                                                    value: $deeperInArray
                                                }
                                            }
                                        ]
                                    }
                                }
                                deeperArray: [
                                    {
                                        value: ""CONSTANT_ARRAY"",
                                        deeper: {
                                            value: $deeperArray
                                        }
                                    }
                                    $complex
                                ]
                            }
                        )
                        contracts {
                            id
                            ... on LifeInsuranceContract {
                                premium
                            }
                            ... on SomeOtherContract {
                                expiryDate
                            }
                        }
                    }
                }",
                variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_Union()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Customer {
                            contracts: [Contract!]
                                @delegate(
                                    schema: ""contract"",
                                    path: ""contracts(customerId:$fields:id)"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    customer: customerOrConsultant(id: ""Q3VzdG9tZXIKZDE="") {
                        ...customer
                        ...consultant
                    }
                    consultant: customerOrConsultant(id: ""Q29uc3VsdGFudApkMQ=="") {
                        ...customer
                        ...consultant
                    }
                }

                fragment customer on Customer {
                    name
                    consultant {
                        name
                    }
                    contracts {
                        id
                        ... on LifeInsuranceContract {
                            premium
                        }
                        ... on SomeOtherContract {
                            expiryDate
                        }
                    }
                }

                fragment consultant on Consultant {
                    name
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_Arguments()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Customer {
                            contracts: [Contract!]
                                @delegate(
                                    schema: ""contract"",
                                    path: ""contracts(customerId:$fields:id)"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    contracts(id: ""Q3VzdG9tZXIKZDE="") {
                        id
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_List_Aggregations()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Customer {
                            contractIds: [ID!]
                                @delegate(
                                    schema: ""contract"",
                                    path: ""contracts(customerId:$fields:id).id"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    customer(id: ""Q3VzdG9tZXIKZDE="") {
                        contractIds
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_Object_Aggregations()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Query {
                            consultant: Consultant
                                @delegate(
                                    schema: ""customer""
                                    path: ""customer(id:\""Q3VzdG9tZXIKZDE=\"").consultant"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    consultant {
                        name
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_Scalar_Aggregations()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Query {
                            consultantName: String!
                                @delegate(
                                    schema: ""customer""
                                    path: ""customer(id:\""Q3VzdG9tZXIKZDE=\"").consultant.name"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    consultantName
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_Computed()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Customer {
                            foo: String @computed(dependantOn: [""id"", ""name""])
                        }")
                    .MapField(
                        new FieldReference("Customer", "foo"),
                        next => context =>
                        {
                            var obj = context.Parent<IReadOnlyDictionary<string, object>>();
                            context.Result = obj["name"] + "_" + obj["id"];
                            return default;
                        })
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    customer(id: ""Q3VzdG9tZXIKZDE="") {
                        foo
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_RenameScalar()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddType(new FloatType("Foo"))
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .RenameType("Float", "Foo")
                    .AddTypeExtensionsFromString(
                        @"extend type Customer {
                            contracts: [Contract!]
                                @delegate(
                                    schema: ""contract"",
                                    path: ""contracts(customerId:$fields:id)"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            var variables = new Dictionary<string, object>
            {
                { "v", new FloatValueNode(1.2f) }
            };

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"query ($v: Foo) {
                    customer: customerOrConsultant(id: ""Q3VzdG9tZXIKZDE="") {
                        ...customer
                        ...consultant
                    }
                    consultant: customerOrConsultant(id: ""Q29uc3VsdGFudApkMQ=="") {
                        ...customer
                        ...consultant
                    }
                }

                fragment customer on Customer {
                    name
                    consultant {
                        name
                    }
                    contracts {
                        id
                        ... on LifeInsuranceContract {
                            premium
                            a: float_field(f: 1.1)
                            b: float_field(f: $v)
                        }
                        ... on SomeOtherContract {
                            expiryDate
                        }
                    }
                }

                fragment consultant on Consultant {
                    name
                }",
                variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_IntField()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Customer {
                            int: Int!
                                @delegate(
                                    schema: ""contract"",
                                    path: ""int(i:$fields:someInt)"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    customer(id: ""Q3VzdG9tZXIKZDE="") {
                        int
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute_GuidField()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Customer {
                            guid: Uuid!
                                @delegate(
                                    schema: ""contract"",
                                    path: ""guid(guid:$fields:someGuid)"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    customer(id: ""Q3VzdG9tZXIKZDE="") {
                        guid
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Foo()
        {
            // "/Users/michael/local/PublicSpeaking/channel9_2020/3-schema-stitching/gateway/"

            var services = new ServiceCollection();

            services.AddHttpClient("accounts", c => c.BaseAddress = new Uri("http://localhost:5051/graphql"));
            services.AddHttpClient("inventory", c => c.BaseAddress = new Uri("http://localhost:5052/graphql"));
            services.AddHttpClient("products", c => c.BaseAddress = new Uri("http://localhost:5053/graphql"));
            services.AddHttpClient("review", c => c.BaseAddress = new Uri("http://localhost:5054/graphql"));

            var builder = services
                .AddGraphQLServer()
                .AddRemoteSchema("accounts", ignoreRootTypes: true)
                .AddRemoteSchema("inventory", ignoreRootTypes: true)
                .AddRemoteSchema("products", ignoreRootTypes: true)
                // .AddTypeExtensionsFromString("type Query { }")
                // .AddRemoteSchema("review", ignoreRootTypes: true)
                .AddHttpRequestInterceptor((httpContext, executor, builder, ct) =>
                {
                    builder.SetProperty("userId", 1);
                    return default;
                })
                .AddTypeExtensionsFromString("type Query { }")
                .AddTypeExtensionsFromFile("/users/michael/local/PublicSpeaking/channel9_2020/3-schema-stitching/accounts/Stitching.graphql")
                .AddTypeExtensionsFromFile("/users/michael/local/PublicSpeaking/channel9_2020/3-schema-stitching/products/Stitching.graphql")
                .AddTypeExtensionsFromFile("/users/michael/local/PublicSpeaking/channel9_2020/3-schema-stitching/inventory/Stitching.graphql");

            IRequestExecutor executor = await builder.BuildRequestExecutorAsync();
            // .AddTypeExtensionsFromFile("../reviews/Stitching.graphql");

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                  topProducts(first: 5) {
                    inStock
                    shippingEstimate
                  }
                }");

            // assert
            result.MatchSnapshot();
        }
    }
}
