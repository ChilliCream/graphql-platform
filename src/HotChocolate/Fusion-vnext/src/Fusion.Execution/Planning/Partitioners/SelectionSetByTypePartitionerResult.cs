using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal sealed record SelectionSetByTypePartitionerResult(
    SelectionSetNode? SharedSelectionSet,
    ImmutableArray<SelectionSetByType> SelectionSetsByType,
    ISelectionSetIndex SelectionSetIndex);

internal sealed record SelectionSetByType(FusionObjectTypeDefinition Type, SelectionSetNode SelectionSet);
