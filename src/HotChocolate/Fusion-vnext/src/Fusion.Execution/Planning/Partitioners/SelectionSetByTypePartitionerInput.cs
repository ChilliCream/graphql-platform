namespace HotChocolate.Fusion.Planning.Partitioners;

internal readonly ref struct SelectionSetByTypePartitionerInput
{
    public required SelectionSet SelectionSet { get; init; }
    public required ISelectionSetIndex SelectionSetIndex { get; init; }
}
