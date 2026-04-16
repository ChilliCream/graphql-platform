using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using RequestDelegate = HotChocolate.Execution.RequestDelegate;

namespace HotChocolate.CostAnalysis;

public sealed class OneOfCostTests
{
    [Fact]
    public async Task OneOfVariable_NoFields()
    {
        // arrange
        const string schema =
            """
            directive @oneOf on INPUT_OBJECT
            directive @cost(weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            input OneOfSample @oneOf {
              fieldOne: String @cost(weight: "900")
              fieldTwo: String @cost(weight: "400")
            }

            type Mutation { setField(input: OneOfSample): String }
            type Query { dummy: String }
            """;

        const string operation =
            """
            mutation($input: OneOfSample) { setField(input: $input) }
            """;

        var request = OperationRequestBuilder.New().SetDocument(operation)
            .SetVariableValues("""
                               {
                                 "input": { "fieldTwo": "a" }
                               }
                               """)
            .ReportCost()
            .Build();

        var executor = await CreateRequestExecutor(schema);

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        // 0 (field) + 1 (arg) + max(400, 900) = 901
        Assert.Equal(901, (int)GetFieldCost(result));
    }

    [Fact]
    public async Task OneOfInput_WithFieldValue()
    {
        // arrange
        const string schema =
            """
            directive @oneOf on INPUT_OBJECT
            directive @cost(weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            input OneOfSample @oneOf {
              fieldOne: String @cost(weight: "800")
              fieldTwo: String @cost(weight: "400")
            }

            type Mutation { setField(input: OneOfSample): String }
            type Query { dummy: String }
            """;

        const string operation =
            """
            mutation { setField(input: { fieldOne: "a" }) }
            """;

        var request = OperationRequestBuilder.New().SetDocument(operation).ReportCost().Build();

        var executor = await CreateRequestExecutor(schema);

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        // 0 (field) + 1 (arg) + max(400, 800) = 801
        Assert.Equal(801, (int)GetFieldCost(result));
    }

    [Fact]
    public async Task NestedOneOfVariable_WithNestedFieldValue()
    {
        // arrange
        const string schema =
            """
            directive @oneOf on INPUT_OBJECT
            directive @cost(weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            input OneOfSample @oneOf {
              fieldOne: String @cost(weight: "900")
              fieldTwo: String @cost(weight: "400")
            }

            input NestedOneOfSample @oneOf {
              fieldOne: OneOfSample @cost(weight: "900")  # Cost of OneOfSample: 900 + 900 for field = 1800
              fieldTwo: NormalType @cost(weight: "550")   # Cost of NormalType: 1300 + 550 for field = 1850
            }

            input NormalType {
              fieldOne: String @cost(weight: "900")
              fieldTwo: String @cost(weight: "400")
            }

            type Mutation { setFieldNested(input: NestedOneOfSample): String }
            type Query { dummy: String }
            """;

        const string operation =
            """
            mutation($input: NestedOneOfSample) { setFieldNested(input: $input) }
            """;

        var request = OperationRequestBuilder.New()
            .SetDocument(operation)
            .SetVariableValues("""
                               {
                                 "input": {
                                   "fieldOne": {
                                     "fieldTwo": "a"
                                   }
                                 }
                               }
                               """)
            .ReportCost()
            .Build();

        var executor = await CreateRequestExecutor(schema);

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        // 0 (field) + 1 (arg) + max(fieldOne: 900+max(900,400)=1800, fieldTwo: 550+(900+400)=1850) = 1851
        Assert.Equal(1851, (int)GetFieldCost(result));
    }

    [Fact]
    public async Task NestedOneOfInput_WithNestedFieldValue()
    {
        // arrange
        const string schema =
            """
            directive @oneOf on INPUT_OBJECT
            directive @cost(weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            input OneOfSample @oneOf {
              fieldOne: String @cost(weight: "900")
              fieldTwo: String @cost(weight: "400")
            }

            input NestedOneOfSample @oneOf {
              fieldOne: OneOfSample @cost(weight: "900")  # Cost of OneOfSample: 900 + 900 for field = 1800
              fieldTwo: NormalType @cost(weight: "550")   # Cost of NormalType: 1300 + 550 for field = 1850
            }

            input NormalType {
              fieldOne: String @cost(weight: "900")
              fieldTwo: String @cost(weight: "400")
            }

            type Mutation { setFieldNested(input: NestedOneOfSample): String }
            type Query { dummy: String }
            """;

        const string operation =
            """
            mutation {
              setFieldNested(input: { fieldOne: { fieldTwo: "a" } })
            }
            """;

        var request = OperationRequestBuilder.New()
            .SetDocument(operation)
            .ReportCost()
            .Build();

        var executor = await CreateRequestExecutor(schema);

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        // 0 (field) + 1 (arg) + NestedOneOfSample.fieldOne(900) + OneOfSample.fieldTwo(400) = 1301
        Assert.Equal(1301, (int)GetFieldCost(result));
    }

    [Fact]
    public async Task NestedOneOf_Variable_Uses_Max_Cost()
    {
        // arrange
        const string schema =
            """
            directive @oneOf on INPUT_OBJECT
            directive @cost(weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            input InnerOneOf @oneOf {
              a: String @cost(weight: "200")
              b: String @cost(weight: "300")
            }

            input OuterOneOf @oneOf {
              inner: InnerOneOf
              c: String @cost(weight: "500")
            }

            type Mutation { setField(input: OuterOneOf): String }
            type Query { dummy: String }
            """;

        const string operation =
            """
            mutation($input: OuterOneOf) { setField(input: $input) }
            """;

        var executor = await CreateRequestExecutor(schema);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(operation).ReportCost().Build());

        // assert
        // 0 (field) + 1 (arg) + max(301, 500) = 501
        Assert.Equal(501, (int)GetFieldCost(result));
    }

    [Fact]
    public async Task OneOf_ContainingRegularObject_Variable_Uses_Max_Cost()
    {
        // arrange
        const string schema =
            """
            directive @oneOf on INPUT_OBJECT
            directive @cost(weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            input ComplexObject {
              x: String @cost(weight: "200")
              y: String @cost(weight: "300")
            }

            input OuterOneOf @oneOf {
              complex: ComplexObject
              simple: String @cost(weight: "400")
            }

            type Mutation { setField(input: OuterOneOf): String }
            type Query { dummy: String }
            """;

        const string operation =
            """
            mutation($input: OuterOneOf) { setField(input: $input) }
            """;

        var executor = await CreateRequestExecutor(schema);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(operation).ReportCost().Build());

        // assert
        // 0 (field) + 1 (arg) + max(501, 400) = 502
        Assert.Equal(502, (int)GetFieldCost(result));
    }

    [Fact]
    public async Task ListOfOneOf_Variable_Uses_MaxPerItem()
    {
        // arrange
        const string schema =
            """
            directive @oneOf on INPUT_OBJECT
            directive @cost(weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            input ItemOneOf @oneOf {
              a: String @cost(weight: "100")
              b: String @cost(weight: "200")
            }

            type Mutation { process(items: [ItemOneOf!]!): String }
            type Query { dummy: String }
            """;

        const string operation =
            """
            mutation($items: [ItemOneOf!]!) { process(items: $items) }
            """;

        var executor = await CreateRequestExecutor(schema);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .SetVariableValues("""{ "items": [{ "a": "v" }] }""")
                .ReportCost()
                .Build());

        // assert
        // 0 (field) + 1 (arg) + max(100, 200) = 201
        Assert.Equal(201, (int)GetFieldCost(result));
    }

    [Fact]
    public async Task ListOfOneOf_InlineValues_UsesActualFieldCosts()
    {
        // arrange
        // three items: {a}, {b}, {a} -> field costs = 100 + 200 + 100
        const string schema =
            """
            directive @oneOf on INPUT_OBJECT
            directive @cost(weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            input ItemOneOf @oneOf {
              a: String @cost(weight: "100")
              b: String @cost(weight: "200")
            }

            type Mutation { process(items: [ItemOneOf!]!): String }
            type Query { dummy: String }
            """;

        const string operation =
            """
            mutation {
              process(items: [{ a: "v" }, { b: "v" }, { a: "v" }])
            }
            """;

        var executor = await CreateRequestExecutor(schema);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(operation).ReportCost().Build());

        // assert
        // 0 (field) + 1 (arg) + (100 + 200 + 100) = 401
        Assert.Equal(401, (int)GetFieldCost(result));
    }

    private static ValueTask<IRequestExecutor> CreateRequestExecutor(string schema)
    {
        return new ServiceCollection()
            .AddGraphQLServer()
            .ModifyCostOptions(o => o.DefaultResolverCost = null)
            .AddDirectiveType<Types.CostDirectiveType>()
            .AddDirectiveType<Types.ListSizeDirectiveType>()
            .AddResolver("Mutation", "setField", _ => "ok")
            .AddResolver("Mutation", "process", _ => "ok")
            .AddResolver("Mutation", "setFieldNested", _ => "ok")
            .AddResolver("Query", "dummy", _ => "ok")
            .AddDocumentFromString(schema)
            .BuildRequestExecutorAsync();
    }

    private static double GetFieldCost(IExecutionResult result)
    {
        var operationResult = result.ExpectOperationResult();
        var metrics = operationResult.Extensions["operationCost"] as IReadOnlyDictionary<string, object>;
        Assert.NotNull(metrics);
        return Convert.ToDouble(metrics["fieldCost"]);
    }
}
