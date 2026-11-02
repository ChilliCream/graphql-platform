namespace Mocha.Sagas.Tests;

public static class SagaTesterFluentPlanExtensions
{
    /// <summary>
    /// Creates a new SagaTestPlan for the given SagaTester.
    /// </summary>
    public static SagaTestPlan<T> Plan<T>(this SagaTester<T> tester) where T : SagaStateBase
    {
        return new SagaTestPlan<T>(tester);
    }
}
