using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class EventStreamSource
{
    internal required string SchemaName { get; init; }

    internal required string FieldName { get; init; }

    internal required ImmutableArray<string> Topics { get; init; }

    internal string? Broker { get; init; }

    internal required SelectionSetNode Message { get; init; }

    internal string? CursorField { get; init; }

    internal string? CursorArgument { get; init; }
}
