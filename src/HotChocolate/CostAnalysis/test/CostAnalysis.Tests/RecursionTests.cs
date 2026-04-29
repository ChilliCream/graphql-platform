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

        var executor = await CostAnalysisTestHelper.CreateRequestExecutor(schema);

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        // FooInput: bar(1001) + next(1002) + [FooInput truncated→0] = 2003
        // 0 (field) + 1 (arg) + 2003 = 2004
        Assert.Equal(2004, (int)CostAnalysisTestHelper.GetFieldCost(result));
    }

    [Fact]
    public async Task MutuallyRecursiveInput_Variables_OrderIndependent()
    {
        // arrange
        const string schema =
            """
            directive @cost(weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            input A {
              b: B @cost(weight: "10")
              aField: String @cost(weight: "100")
            }

            input B {
              a: A @cost(weight: "20")
              bField: String @cost(weight: "200")
            }

            type Mutation { setBoth(a: A, b: B): String }
            type Query { dummy: String }
            """;

        const string operationAFirst =
            """
            mutation($a: A, $b: B) { setBoth(a: $a, b: $b) }
            """;

        const string operationBFirst =
            """
            mutation($b: B, $a: A) { setBoth(b: $b, a: $a) }
            """;

        var executor = await CostAnalysisTestHelper.CreateRequestExecutor(schema);

        // act
        var resultAFirst = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(operationAFirst).ReportCost().Build());

        var resultBFirst = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(operationBFirst).ReportCost().Build());

        // assert
        // A and B are mutually recursive, the B subtree reached from $a is recomputed instead of being cached:
        // $a → A: aField(100) + b(10) + B: bField(200) + a(20) = 330
        // $b → B: bField(200) + a(20) + A: aField(100) + b(10) = 330
        // Total: 0 (field) + 1 (arg $a) + 330 + 1 (arg $b) + 330 = 662
        const int expectedCost = 662;

        var costAFirst = (int)CostAnalysisTestHelper.GetFieldCost(resultAFirst);
        var costBFirst = (int)CostAnalysisTestHelper.GetFieldCost(resultBFirst);

        Assert.Equal(expectedCost, costAFirst);
        Assert.Equal(expectedCost, costBFirst);
    }
}
