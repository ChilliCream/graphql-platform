using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal record SelectionSetPartitionerResult(
    SelectionSetNode? Resolvable,
    ImmutableStack<SelectionSet> Unresolvable,
    ImmutableStack<FieldSelection> FieldsWithRequirements,
    ISelectionSetIndex SelectionSetIndex);
