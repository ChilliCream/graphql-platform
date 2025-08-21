using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal readonly ref struct SelectionSetPartitionerInput
{
    public required string SchemaName { get; init; }
    public required SelectionSet SelectionSet { get; init; }
    public required ISelectionSetIndex SelectionSetIndex { get; init; }
    public SelectionSetNode? ProvidedSelectionSetNode { get; init; }
    public bool AllowRequirements { get; init; }
}
