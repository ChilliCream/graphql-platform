namespace HotChocolate.Fusion.Planning;

internal readonly ref struct SelectionSetByTypePartitionerInput
{
    public required SelectionSet SelectionSet { get; init; }
    public required ISelectionSetIndex SelectionSetIndex { get; init; }
}
