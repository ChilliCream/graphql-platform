using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal sealed record RootSelectionSetPartitionerResult(
    SelectionSet? SelectionSet,
    List<FieldNode>? NodeFields,
    ISelectionSetIndex SelectionSetIndex);
