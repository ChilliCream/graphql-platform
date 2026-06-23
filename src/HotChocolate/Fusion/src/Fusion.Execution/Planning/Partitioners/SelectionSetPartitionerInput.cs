using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal readonly ref struct SelectionSetPartitionerInput
{
    public required string SchemaName { get; init; }
    public required SelectionSet SelectionSet { get; init; }
    public required ISelectionSetIndex SelectionSetIndex { get; init; }
    public SelectionSetNode? ProvidedSelectionSet { get; init; }
    public bool PruneUnprovidedAbstractBranches { get; init; }
}
