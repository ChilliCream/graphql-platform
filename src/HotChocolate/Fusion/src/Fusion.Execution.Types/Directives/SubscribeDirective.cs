using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

/*
directive @fusion__subscribe(
    schema: fusion__Schema!
    topics: [String!]
    broker: String
    message: fusion__FieldSelectionSet!
    cursorField: String
    cursorArgument: String
) on FIELD_DEFINITION
*/
public sealed class SubscribeDirective(
    ImmutableArray<string> topics,
    string? broker,
    SelectionSetNode message,
    string? cursorField = null,
    string? cursorArgument = null)
{
    internal SchemaKey SchemaKey { get; init; }

    public ImmutableArray<string> Topics { get; } = topics;

    public string? Broker { get; } = broker;

    public SelectionSetNode Message { get; } = message;

    public string? CursorField { get; } = cursorField;

    public string? CursorArgument { get; } = cursorArgument;
}
