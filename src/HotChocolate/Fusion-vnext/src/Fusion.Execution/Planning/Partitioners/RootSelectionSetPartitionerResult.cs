namespace HotChocolate.Fusion.Planning.Partitioners;

internal sealed record RootSelectionSetPartitionerResult(
    SelectionSet? SelectionSet,
    List<NodeField>? NodeFields,
    ISelectionSetIndex SelectionSetIndex);
