using HotChocolate.Execution;

namespace HotChocolate.CostAnalysis;

public class RecursionTests
{
    [Fact]
    public async Task RecursiveInput_UsesSingleDepth()
    {
        // arrange
        const string schema =
            """
            directive @cost(weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            input FooInput {
              bar: String @cost(weight: "1001")
              next: FooInput @cost(weight: "1002")
            }

            type Mutation { setField(input: FooInput): String }
            type Query { dummy: String }
            """;

        const string operation =
            """
            mutation m($input: FooInput) {
              setField(input: $input)
            }
            """;

        var request = OperationRequestBuilder.New()
            .SetDocument(operation)
            .SetVariableValues(
                """
                {
                  "input": {
                    "bar": "test"
                  }
                }
                """)
            .ReportCost()
            .Build();

        var executor = await OneOfCostTests.CreateRequestExecutor(schema);

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        // 0 (field) + 1 (arg) + bar: 1001 + next: 1002  = 2004
        Assert.Equal(2004, (int)OneOfCostTests.GetFieldCost(result));
    }
}
