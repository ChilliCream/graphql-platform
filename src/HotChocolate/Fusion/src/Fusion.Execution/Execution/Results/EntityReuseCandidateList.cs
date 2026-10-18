using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// Describes where in the composite result a lookup may find entities it would otherwise
/// fetch from the source schema.
/// </summary>
internal sealed class EntityReuseCandidateList(SelectionPath[] paths, string[] responseNames)
{
    /// <summary>
    /// Gets the result paths of entities that may hold every field the lookup selects.
    /// </summary>
    public ReadOnlySpan<SelectionPath> Paths => paths;

    /// <summary>
    /// Gets the response names of the leaf fields the lookup selects.
    /// </summary>
    public ReadOnlySpan<string> ResponseNames => responseNames;
}
