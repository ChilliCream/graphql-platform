using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// The parts of the path item holding an object that a visit reads, with the provided selection
/// set compared by syntax.
/// </summary>
/// <param name="Field">The field of the path item.</param>
/// <param name="Type">The type declaring the field of the path item.</param>
/// <param name="ProvidedSelectionSet">
/// The provided selection set of the path item, or <c>null</c> if it has none.
/// </param>
/// <param name="ProvidedByEventStream">Whether the provided selection set is an event stream message.</param>
internal sealed record VisitLeaf(
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    ProvidedSelectionSetKey? ProvidedSelectionSet,
    bool ProvidedByEventStream);
