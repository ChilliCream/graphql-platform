using Mocha.Sagas;

namespace Mocha.EntityFrameworkCore.SqlServer.Tests.Helpers;

internal sealed class TestSaga : Saga<TestSagaState>
{
    public TestSaga()
    {
        Name = "test-saga";
        StateSerializer = new JsonSagaStateSerializer(TestSagaStateJsonContext.Default.TestSagaState);
    }

    protected override void Configure(ISagaDescriptor<TestSagaState> descriptor)
    {
        // Minimal config - not needed for store tests.
    }
}
