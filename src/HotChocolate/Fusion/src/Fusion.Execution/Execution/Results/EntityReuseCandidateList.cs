using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// Describes where in the composite result a lookup may find entities it would otherwise
/// fetch from the source schema.
/// </summary>
internal sealed class EntityReuseCandidateList(
    SelectionPath[] paths,
    string[] responseNames,
    string[] keyResponseNames,
    EntityReuseLayout[] layouts)
{
    /// <summary>
    /// Gets the result paths of entities that may hold every field the lookup selects.
    /// </summary>
    public ReadOnlySpan<SelectionPath> Paths => paths;

    /// <summary>
    /// Gets the response names of the leaf fields the lookup selects.
    /// </summary>
    public ReadOnlySpan<string> ResponseNames => responseNames;

    /// <summary>
    /// Gets the response names of the key fields that identify the entities of the lookup.
    /// </summary>
    public ReadOnlySpan<string> KeyResponseNames => keyResponseNames;

    /// <summary>
    /// Gets the row layouts of the selection sets of the targets and of the entities at
    /// <see cref="Paths"/>, or an empty span if the entities cannot be matched by their raw
    /// key values.
    /// </summary>
    public ReadOnlySpan<EntityReuseLayout> Layouts => layouts;
}
