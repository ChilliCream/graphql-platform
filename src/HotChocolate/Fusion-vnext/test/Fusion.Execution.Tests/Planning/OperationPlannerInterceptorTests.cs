using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

public class OperationPlannerInterceptorTests : FusionTestBase
{
    [Fact]
    public void Intercept_Plan_With_Single_Interceptor()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              books: [Book]
            }

            type Book {
              id: String!
              title: String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
                books {
                  title
                }
            }
            """,
            new MockBoolInterceptor(),
            new MockIntInterceptor());

        // assert
        Assert.True(plan.Operation.Features.Get<bool>());
        Assert.Equal(1, plan.Operation.Features.Get<int>());
    }

    [Fact]
    public void Intercept_Plan_With_Multiple_Interceptors()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              books: [Book]
            }

            type Book {
              id: String!
              title: String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
                books {
                  title
                }
            }
            """,
            new MockBoolInterceptor(),
            new MockIntInterceptor());

        // assert
        Assert.True(plan.Operation.Features.Get<bool>());
        Assert.Equal(1, plan.Operation.Features.Get<int>());
    }

    private class MockBoolInterceptor : IOperationPlannerInterceptor
    {
        public void OnAfterPlanCompleted(OperationPlan plan)
        {
            plan.Operation.Features.Set(true);
        }
    }

    private class MockIntInterceptor : IOperationPlannerInterceptor
    {
        public void OnAfterPlanCompleted(OperationPlan plan)
        {
            plan.Operation.Features.Set(1);
        }
    }
}
