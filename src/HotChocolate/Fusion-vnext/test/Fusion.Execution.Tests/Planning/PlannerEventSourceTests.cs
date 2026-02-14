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

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              productBySlug(slug: "1") {
                id
                name
                estimatedDelivery(postCode: "12345")
              }
            }
            """);

        // assert
        var start = listener.Single(PlannerEventSource.PlanStartEventId);
        var stop = listener.Single(PlannerEventSource.PlanStopEventId);

        Assert.Equal("123456789101112", start.GetStringPayload(0));
        Assert.Equal("123456789101112", stop.GetStringPayload(0));

        var elapsedMilliseconds = stop.GetInt64Payload(1);
        var searchSpace = stop.GetInt32Payload(2);
        var expandedNodes = stop.GetInt32Payload(3);
        var stepCount = stop.GetInt32Payload(4);
        var dequeueEvents = listener.ByEventId(PlannerEventSource.PlanDequeueEventId);

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

        // act
        Assert.ThrowsAny<Exception>(
            () => planner.CreatePlan("op-error", "hash1234", "hash1234", operation));

        // assert
        var start = listener.Single(PlannerEventSource.PlanStartEventId);
        var error = listener.Single(PlannerEventSource.PlanErrorEventId);
        var dequeueEvents = listener.ByEventId(PlannerEventSource.PlanDequeueEventId);

        Assert.Equal("op-error", start.GetStringPayload(0));
        Assert.Equal("op-error", error.GetStringPayload(0));
        Assert.Equal("Query", error.GetStringPayload(1));
        Assert.False(string.IsNullOrEmpty(error.GetStringPayload(2)));
        Assert.True(error.GetInt64Payload(3) >= 0);
        Assert.Empty(listener.ByEventId(PlannerEventSource.PlanStopEventId));
        Assert.Empty(dequeueEvents);
    }

    [Fact]
    public void PlannerEventSource_Can_Aggregate_Perf_Metrics_Across_Plans()
    {
        // arrange
        using var listener = new PlannerEventListener();
        var schema = CreateCompositeSchema();

        // act
        PlanOperation(
            schema,
            """
            {
              productBySlug(slug: "1") {
                id
                name
              }
            }
            """);

        PlanOperation(
            schema,
            """
            {
              productBySlug(slug: "1") {
                id
                estimatedDelivery(postCode: "12345")
              }
            }
            """);

        // assert
        var stopEvents = listener.ByEventId(PlannerEventSource.PlanStopEventId);
        var dequeueEvents = listener.ByEventId(PlannerEventSource.PlanDequeueEventId);

        Assert.Equal(2, stopEvents.Count);
        Assert.True(stopEvents.Sum(t => t.GetInt32Payload(2)) >= 2);
        Assert.True(stopEvents.Sum(t => t.GetInt32Payload(3)) >= 2);
        Assert.True(stopEvents.Sum(t => t.GetInt64Payload(1)) >= 0);
        Assert.True(dequeueEvents.Count >= 2);
        Assert.Equal(stopEvents.Sum(t => t.GetInt32Payload(3)), dequeueEvents.Count);
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

        public IReadOnlyList<CapturedEvent> ByEventId(int eventId)
            => _events.Where(t => t.EventId == eventId).ToArray();

        public CapturedEvent Single(int eventId)
            => Assert.Single(ByEventId(eventId));
    }

    private sealed record CapturedEvent(
        int EventId,
        IReadOnlyList<object?> Payload)
    {
        public string GetStringPayload(int index)
            => Assert.IsType<string>(Payload[index]);

        public int GetInt32Payload(int index)
            => Convert.ToInt32(Payload[index], System.Globalization.CultureInfo.InvariantCulture);

        public long GetInt64Payload(int index)
            => Convert.ToInt64(Payload[index], System.Globalization.CultureInfo.InvariantCulture);
    }
}
