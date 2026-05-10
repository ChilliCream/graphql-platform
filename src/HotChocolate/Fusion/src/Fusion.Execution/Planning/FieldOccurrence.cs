using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// A single occurrence of a leaf field in the operation AST, captured during
/// the collector pass together with the enclosing <c>@defer</c> leaf and the
/// active type condition so later passes can group by location and rebuild
/// the wrapping selection set.
/// </summary>
internal sealed record FieldOccurrence(
    ImmutableArray<FieldPathSegment> ParentPath,
    string ResponseName,
    FieldNode FieldNode,
    DeliveryGroup? EnclosingDeliveryGroup,
    NamedTypeNode? TypeCondition);
