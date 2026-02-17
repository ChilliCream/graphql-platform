using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public sealed class PlannerEventSourceTests : FusionTestBase
{
    [Fact]
    public void PlannerEventSource_Emits_Start_And_Stop_With_Perf_Metrics()
    {
        // arrange
        using var listener = new PlannerEventListener();
        var schema = CreateCompositeSchema();
        const string operationId = "planner-etw-start-stop";

        // act
        var plan = CreatePlan(
            schema,
            """
            {
              productBySlug(slug: "1") {
                id
                name
                estimatedDelivery(postCode: "12345")
              }
            }
            """,
            operationId);

        // assert
        var start = listener.Single(PlannerEventSource.PlanStartEventId, operationId);
        var stop = listener.Single(PlannerEventSource.PlanStopEventId, operationId);

        Assert.Equal(operationId, start.GetStringPayload(0));
        Assert.Equal(operationId, stop.GetStringPayload(0));

        var elapsedMilliseconds = stop.GetInt64Payload(1);
        var searchSpace = stop.GetInt32Payload(2);
        var expandedNodes = stop.GetInt32Payload(3);
        var stepCount = stop.GetInt32Payload(4);
        var dequeueEvents = listener.ByEventId(PlannerEventSource.PlanDequeueEventId, operationId);

        Assert.True(elapsedMilliseconds >= 0);
        Assert.Equal((int)plan.SearchSpace, searchSpace);
        Assert.True(expandedNodes > 0);
        Assert.True(stepCount > 0);
        Assert.Equal(expandedNodes, dequeueEvents.Count);
        Assert.Equal(1, dequeueEvents[0].GetInt32Payload(1));
        Assert.Equal(expandedNodes, dequeueEvents[^1].GetInt32Payload(1));
    }

    [Fact]
    public void PlannerEventSource_Emits_Error_When_Planning_Fails()
    {
        // arrange
        using var listener = new PlannerEventListener();
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              me: User!
            }

            type User {
              id: ID!
              name: String!
            }
            """);
        var planner = CreatePlanner(schema);
        var operation = ParseOperation(
            """
            {
              doesNotExist
            }
            """);
        const string operationId = "planner-etw-error";

        // act
        Assert.ThrowsAny<Exception>(
            () => planner.CreatePlan(operationId, "hash1234", "hash1234", operation));

        // assert
        var start = listener.Single(PlannerEventSource.PlanStartEventId, operationId);
        var error = listener.Single(PlannerEventSource.PlanErrorEventId, operationId);
        var dequeueEvents = listener.ByEventId(PlannerEventSource.PlanDequeueEventId, operationId);

        Assert.Equal(operationId, start.GetStringPayload(0));
        Assert.Equal(operationId, error.GetStringPayload(0));
        Assert.Equal("Query", error.GetStringPayload(1));
        Assert.False(string.IsNullOrEmpty(error.GetStringPayload(2)));
        Assert.True(error.GetInt64Payload(3) >= 0);
        Assert.Empty(listener.ByEventId(PlannerEventSource.PlanStopEventId, operationId));
        Assert.Empty(dequeueEvents);
    }

    [Fact]
    public void PlannerEventSource_Can_Aggregate_Perf_Metrics_Across_Plans()
    {
        // arrange
        using var listener = new PlannerEventListener();
        var schema = CreateCompositeSchema();
        const string operationId1 = "planner-etw-aggregate-1";
        const string operationId2 = "planner-etw-aggregate-2";

        // act
        CreatePlan(
            schema,
            """
            {
              productBySlug(slug: "1") {
                id
                name
              }
            }
            """,
            operationId1);

        CreatePlan(
            schema,
            """
            {
              productBySlug(slug: "1") {
                id
                estimatedDelivery(postCode: "12345")
              }
            }
            """,
            operationId2);

        // assert
        var stopEvents = listener.ByEventId(PlannerEventSource.PlanStopEventId, operationId1)
            .Concat(listener.ByEventId(PlannerEventSource.PlanStopEventId, operationId2))
            .ToArray();
        var dequeueEvents = listener.ByEventId(PlannerEventSource.PlanDequeueEventId, operationId1)
            .Concat(listener.ByEventId(PlannerEventSource.PlanDequeueEventId, operationId2))
            .ToArray();

        Assert.Equal(2, stopEvents.Length);
        Assert.True(stopEvents.Sum(t => t.GetInt32Payload(2)) >= 2);
        Assert.True(stopEvents.Sum(t => t.GetInt32Payload(3)) >= 2);
        Assert.True(stopEvents.Sum(t => t.GetInt64Payload(1)) >= 0);
        Assert.True(dequeueEvents.Length >= 2);
        Assert.Equal(stopEvents.Sum(t => t.GetInt32Payload(3)), dequeueEvents.Length);
    }

    private static OperationPlanner CreatePlanner(FusionSchemaDefinition schema)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);

        return new OperationPlanner(schema, compiler);
    }

    private static OperationPlan CreatePlan(
        FusionSchemaDefinition schema,
        [StringSyntax("graphql")] string operationText,
        string operationId)
    {
        var planner = CreatePlanner(schema);
        var operation = ParseOperation(operationText);
        var shortHash = operationId.Length >= 8
            ? operationId[..8]
            : operationId.PadRight(8, '0');

        return planner.CreatePlan(operationId, operationId, shortHash, operation);
    }

    private static OperationDefinitionNode ParseOperation([StringSyntax("graphql")] string operationText)
        => Utf8GraphQLParser.Parse(operationText).Definitions.OfType<OperationDefinitionNode>().First();

    private sealed class PlannerEventListener : EventListener
    {
        private readonly ConcurrentQueue<CapturedEvent> _events = [];

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

            _events.Enqueue(
                new CapturedEvent(
                    eventData.EventId,
                    eventData.Payload is null
                        ? []
                        : [.. eventData.Payload]));
        }

        public IReadOnlyList<CapturedEvent> ByEventId(int eventId, string operationId)
            => _events.Where(t => t.EventId == eventId && t.HasOperationId(operationId)).ToArray();

        public CapturedEvent Single(int eventId, string operationId)
            => Assert.Single(ByEventId(eventId, operationId));
    }

    private sealed record CapturedEvent(
        int EventId,
        IReadOnlyList<object?> Payload)
    {
        public bool HasOperationId(string operationId)
            => Payload.Count > 0
                && Payload[0] is string payloadOperationId
                && payloadOperationId.Equals(operationId, StringComparison.Ordinal);

        public string GetStringPayload(int index)
            => Assert.IsType<string>(Payload[index]);

        public int GetInt32Payload(int index)
            => Convert.ToInt32(Payload[index], System.Globalization.CultureInfo.InvariantCulture);

        public long GetInt64Payload(int index)
            => Convert.ToInt64(Payload[index], System.Globalization.CultureInfo.InvariantCulture);
    }
}
