using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal readonly ref struct SelectionSetPartitionerInput
{
    public required string SchemaName { get; init; }
    public required SelectionSet SelectionSet { get; init; }
    public required ISelectionSetIndex SelectionSetIndex { get; init; }
    public SelectionSetNode? ProvidedSelectionSet { get; init; }
    public bool PruneUnprovidedAbstractBranches { get; init; }
    public bool TreatSourceExternalAsUnresolvable { get; init; }

    /// <summary>
    /// The entering lookup's key selection set when the partitioned selection set sits at a
    /// lookup entry root. While this scope is active, a sourceExternal field (an Apollo
    /// Federation @external key field promoted by composition) is only resolvable when the
    /// key covers it, because the source schema can merely echo the representation it was
    /// entered with. <c>null</c> deactivates the mechanism.
    /// </summary>
    public SelectionSetNode? EntryKeyCoverage { get; init; }
}
