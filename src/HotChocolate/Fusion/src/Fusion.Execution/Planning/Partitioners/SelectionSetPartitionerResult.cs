using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal record SelectionSetPartitionerResult(
    SelectionSetNode? Resolvable,
    ImmutableStack<ConditionedSelectionSet> Unresolvable,
    ImmutableStack<ConditionedFieldSelection> FieldsWithRequirements,
    ImmutableStack<ConditionalPolicyExecutionTarget> PolicyTargets,
    ISelectionSetIndex SelectionSetIndex);
