using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal sealed record SelectionSetByType(
    FusionObjectTypeDefinition Type,
    SelectionSetNode SelectionSet);
