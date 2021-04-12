using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using HotChocolate.Tests;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;
using ChilliCream.Testing;
using System;
using HotChocolate.Execution.Configuration;

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
        public async Task AutoMerge_Schema_Preserves_Scalar_Directives()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            // act
            ISchema schema =
                await new ServiceCollection()
                   .AddSingleton(httpClientFactory)
                   .AddGraphQL()
                   .AddRemoteSchemaFromString("Simple", FileResource.Open("SimpleSchema.graphql"))
                   .AddType(new AnyType("CustomScalar"))
                   .AddGraphQL("Simple")
                   .AddType(new AnyType("CustomScalar"))
                   .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
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
        public async Task Directive_Delegation()
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
                    contracts @include(if: true) {
                        id
                        ... on LifeInsuranceContract {
                            premium
                        }
                    }
                    contracts @include(if: true) {
                        id
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
        public async Task AutoMerge_Execute_Customer_DoesNotExist_And_Is_Correctly_Null()
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
                    customer(id: ""Q3VzdG9tZXIKaTI5OTk="") {
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
        public async Task Add_Dummy_Directive()
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
                    .AddTypeExtensionsFromString(
                        @"directive @foo on FIELD_DEFINITION")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildSchemaAsync();

            // assert
            Assert.NotNull(schema.GetDirectiveType("foo"));
        }

        [Fact]
        public async Task Add_Dummy_Directive_From_Resource()
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
                    .AddTypeExtensionsFromResource(
                        GetType().Assembly,
                        "HotChocolate.Stitching.__resources__.DummyDirective.graphql")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildSchemaAsync();

            // assert
            Assert.NotNull(schema.GetDirectiveType("foo"));
        }

        [Fact]
        public async Task Add_Dummy_Directive_From_Resource_Key_Does_Not_Exist()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            // act
            async Task Configure() =>
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromResource(
                        GetType().Assembly,
                        "HotChocolate.Stitching.__resources__.abc")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildSchemaAsync();

            // assert
            SchemaException exception = await Assert.ThrowsAsync<SchemaException>(Configure);
            Assert.Contains(
                "The resource `HotChocolate.Stitching.__resources__.abc` was not found!",
                exception.Message);
        }

        [Fact]
        public async Task AddLocalSchema()
        {
            // arrange
            var connections = new Dictionary<string, HttpClient>
            {
                { Context.ContractSchema, Context.CreateContractService().CreateClient() }
            };

            IHttpClientFactory httpClientFactory =
                StitchingTestContext.CreateRemoteSchemas(connections);

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddLocalSchema(Context.CustomerSchema)
                    .AddTypeExtensionsFromString(
                        @"extend type Customer {
                            contracts: [Contract!]
                                @delegate(
                                    schema: ""contract"",
                                    path: ""contracts(customerId:$fields:id)"")
                        }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .AddGraphQL(Context.CustomerSchema)
                    .AddCustomerSchema()
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
    }
}
