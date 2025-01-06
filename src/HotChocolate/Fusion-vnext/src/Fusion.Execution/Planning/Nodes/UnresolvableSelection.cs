using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed record UnresolvableSelection(
    ISelectionNode Selection,
    ImmutableStack<SelectionPlanNode> Path);


// we need also to track:
// - ITypeNode type
// - FieldPath RequiredField
public sealed record DataRequirement(
    SelectionSetNode SelectionSet,
    ImmutableArray<FieldReference> RequiredFields,
    ImmutableStack<SelectionPlanNode> Path);

// name is not good ...
public sealed record FieldReference(
    // what is required
    SelectionPath Path,

    // whats the type of the data that is required
    ITypeNode Type);
