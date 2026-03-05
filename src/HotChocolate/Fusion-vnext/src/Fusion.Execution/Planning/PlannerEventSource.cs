using System.Diagnostics.Tracing;

namespace HotChocolate.Fusion.Planning;

[EventSource(Name = EventSourceName)]
internal sealed class PlannerEventSource : EventSource
{
    public const string EventSourceName = "HotChocolate-Fusion-Planner";

    public const int PlanStartEventId = 1;
    public const int PlanStopEventId = 2;
    public const int PlanErrorEventId = 3;
    public const int PlanDequeueEventId = 4;
    public const int PlanGuardrailExceededEventId = 5;

    public static readonly PlannerEventSource Log = new();

    private PlannerEventSource() { }

    [Event(
        eventId: PlanStartEventId,
        Level = EventLevel.Informational,
        Opcode = EventOpcode.Start,
        Message = "Planner started (OperationId={0}, OperationType={1}, RootSelectionCount={2})")]
    public void PlanStart(string operationId, string operationType, int rootSelectionCount)
    {
        if (IsEnabled())
        {
            WriteEvent(PlanStartEventId, operationId, operationType, rootSelectionCount);
        }
    }

    [Event(
        eventId: PlanStopEventId,
        Level = EventLevel.Informational,
        Opcode = EventOpcode.Stop,
        Message = "Planner completed (OperationId={0}, ElapsedMs={1}, SearchSpace={2}, ExpandedNodes={3}, StepCount={4})")]
    public void PlanStop(
        string operationId,
        long elapsedMilliseconds,
        int searchSpace,
        int expandedNodes,
        int stepCount)
    {
        if (IsEnabled())
        {
            WriteEvent(
                PlanStopEventId,
                operationId,
                elapsedMilliseconds,
                searchSpace,
                expandedNodes,
                stepCount);
        }
    }

    [Event(
        eventId: PlanErrorEventId,
        Level = EventLevel.Error,
        Message = "Planner failed (OperationId={0}, OperationType={1}, ErrorType={2}, ElapsedMs={3})")]
    public void PlanError(
        string operationId,
        string operationType,
        string errorType,
        long elapsedMilliseconds)
    {
        if (IsEnabled())
        {
            WriteEvent(
                PlanErrorEventId,
                operationId,
                operationType,
                errorType,
                elapsedMilliseconds);
        }
    }

    [Event(
        eventId: PlanDequeueEventId,
        Level = EventLevel.Verbose,
        Message = "Planner dequeue (OperationId={0}, Cycle={1}, QueueLength={2}, NextWorkItem={3}, SchemaName={4})")]
    public void PlanDequeue(
        string operationId,
        int cycle,
        int queueLength,
        string nextWorkItem,
        string schemaName)
    {
        if (IsEnabled())
        {
            WriteEvent(
                PlanDequeueEventId,
                operationId,
                cycle,
                queueLength,
                nextWorkItem,
                schemaName);
        }
    }

    [Event(
        eventId: PlanGuardrailExceededEventId,
        Level = EventLevel.Warning,
        Message = "Planner guardrail exceeded (OperationId={0}, Reason={1}, Limit={2}, Observed={3})")]
    public void PlanGuardrailExceeded(
        string operationId,
        string reason,
        long limit,
        long observed)
    {
        if (IsEnabled())
        {
            WriteEvent(
                PlanGuardrailExceededEventId,
                operationId,
                reason,
                limit,
                observed);
        }
    }
}
