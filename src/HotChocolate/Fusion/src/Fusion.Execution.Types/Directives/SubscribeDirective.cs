using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

/*
directive @fusion__subscribe(
    schema: fusion__Schema!
    topics: [String!]
    broker: String
    message: fusion__FieldSelectionSet!
) on FIELD_DEFINITION
*/
public sealed class SubscribeDirective(
    ImmutableArray<string> topics,
    string? broker,
    SelectionSetNode message)
{
    internal SchemaKey SchemaKey { get; init; }

    public ImmutableArray<string> Topics { get; } = topics;

    public string? Broker { get; } = broker;

    public SelectionSetNode Message { get; } = message;
}
