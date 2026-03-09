using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public sealed class OperationPlannerCancellationTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Throws_When_CancellationToken_Is_Already_Canceled()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var planner = CreatePlanner(schema);
        var operation = ParseOperation(TestOperationText);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // act
        var error = Assert.ThrowsAny<OperationCanceledException>(
            () => planner.CreatePlan("planner-cancel-before-start", "hash", "hash", operation, cts.Token));

        // assert
        Assert.Equal(cts.Token, error.CancellationToken);
    }

    [Fact]
    public void CreatePlan_Throws_When_CancellationToken_Is_Canceled_During_Planning()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var planner = CreatePlanner(schema);
        var operation = ParseOperation(TestOperationText);
        const string operationId = "planner-cancel-during-planning";
        using var cts = new CancellationTokenSource();
        using var listener = new CancelOnFirstDequeueEventListener(operationId, cts);

        // act
        Assert.ThrowsAny<OperationCanceledException>(
            () => planner.CreatePlan(operationId, "hash", "hash", operation, cts.Token));

        // assert
        Assert.True(cts.IsCancellationRequested);
        Assert.True(listener.DequeueCount > 0);
    }

    private static OperationPlanner CreatePlanner(FusionSchemaDefinition schema)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);

        return new OperationPlanner(schema, compiler);
    }

    private static OperationDefinitionNode ParseOperation([StringSyntax("graphql")] string operationText)
        => Utf8GraphQLParser.Parse(operationText).Definitions.OfType<OperationDefinitionNode>().First();

    private const string TestOperationText =
        """
        {
          productBySlug(slug: "1") {
            id
            name
            estimatedDelivery(postCode: "12345")
          }
        }
        """;

    private sealed class CancelOnFirstDequeueEventListener : EventListener
    {
        private readonly string _operationId;
        private readonly CancellationTokenSource _cts;
        private int _dequeueCount;

        public CancelOnFirstDequeueEventListener(
            string operationId,
            CancellationTokenSource cts)
        {
            _operationId = operationId;
            _cts = cts;
        }

        public int DequeueCount => _dequeueCount;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals(PlannerEventSource.EventSourceName, StringComparison.Ordinal))
            {
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!eventData.EventSource.Name.Equals(PlannerEventSource.EventSourceName, StringComparison.Ordinal))
            {
                return;
            }

            if (eventData.EventId != PlannerEventSource.PlanDequeueEventId
                || eventData.Payload is null
                || eventData.Payload.Count < 1
                || eventData.Payload[0] is not string operationId
                || !operationId.Equals(_operationId, StringComparison.Ordinal))
            {
                return;
            }

            if (Interlocked.Increment(ref _dequeueCount) == 1)
            {
                _cts.Cancel();
            }
        }
    }
}
