using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

internal sealed class EventStreamPlan
{
    internal required string FieldName { get; init; }

    internal required EventStreamSource Source { get; init; }

    internal required string Message { get; init; }
}
