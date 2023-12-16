using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Schemas.Customers;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

namespace HotChocolate.Stitching.Integration;

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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        // act
        var schema =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
                .AddRemoteSchema(Context.ContractSchema)
                .AddRemoteSchema(Context.CustomerSchema)
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
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
    public async Task LocalField_Execute()
    {
        // arrange
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
                .AddTypeExtension(new ObjectTypeExtension(d
                    => d.Name("Query").Field("local").Resolve("I am local.")))
                .AddRemoteSchema(Context.ContractSchema)
                .AddRemoteSchema(Context.CustomerSchema)
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                    local
                    allCustomers {
                        id
                        name
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Schema_AddResolver()
    {
        // arrange
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
                .AddRemoteSchema(Context.ContractSchema)
                .AddRemoteSchema(Context.CustomerSchema)
                .AddResolver("Query", "local", "I am local")
                .AddTypeExtensionsFromString("extend type Query { local: String }")
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                    local
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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

        var variables = new Dictionary<string, object?>
        {
            { "customerId", "Q3VzdG9tZXIKZDE=" },
            { "deep", "deep" }, { "deeper", "deeper" }
        };

        // act
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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

        var variables = new Dictionary<string, object?> { { "v", new FloatValueNode(1.2f) } };

        // act
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
                .AddRemoteSchema(Context.ContractSchema)
                .AddRemoteSchema(Context.CustomerSchema)
                .AddTypeExtensionsFromString(
                    @"extend type Customer {
                            guid: UUID!
                                @delegate(
                                    schema: ""contract"",
                                    path: ""guid(guid:$fields:someGuid)"")
                        }")
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                customer(id: ""Q3VzdG9tZXIKZDE="") {
                    guid
                }
            }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task AutoMerge_Execute_Schema_GuidField()
    {
        // arrange
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        var schema =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
                .AddRemoteSchema(Context.ContractSchema)
                .AddRemoteSchema(Context.CustomerSchema)
                .AddTypeExtensionsFromString(
                    @"extend type Customer {
                            guid: UUID!
                                @delegate(
                                    schema: ""contract"",
                                    path: ""guid(guid:$fields:someGuid)"")
                        }")
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .BuildSchemaAsync();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Add_Dummy_Directive()
    {
        // arrange
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        // act
        var schema =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.RemoveUnusedTypeSystemDirectives = false;
                    o.EnableTag = false;
                })
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        // act
        var schema =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.RemoveUnusedTypeSystemDirectives = false;
                    o.EnableTag = false;
                })
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
        var httpClientFactory =
            Context.CreateDefaultRemoteSchemas();

        // act
        async Task Configure() =>
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
                .AddRemoteSchema(Context.ContractSchema)
                .AddRemoteSchema(Context.CustomerSchema)
                .AddTypeExtensionsFromResource(
                    GetType().Assembly,
                    "HotChocolate.Stitching.__resources__.abc")
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .BuildSchemaAsync();

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(Configure);
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

        var httpClientFactory =
            StitchingTestContext.CreateRemoteSchemas(connections);

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
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
        var result = await executor.ExecuteAsync(
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
