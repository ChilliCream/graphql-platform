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
    /// When partitioning a lookup entry root, limits <c>sourceExternal</c> fields to those
    /// included in the entering lookup key. Set to <c>null</c> to disable this constraint.
    /// </summary>
    public SelectionSetNode? EntryKeyCoverage { get; init; }
}
