using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal sealed record SelectionSetByTypePartitionerResult(
    SelectionSetNode? SharedSelectionSet,
    ImmutableArray<SelectionSetByType> SelectionSetsByType,
    ISelectionSetIndex SelectionSetIndex);
