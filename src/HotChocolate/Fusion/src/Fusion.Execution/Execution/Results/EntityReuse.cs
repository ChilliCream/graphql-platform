using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// Describes where a lookup can take entities from that are already part of the
/// composite result, instead of fetching them from the source schema.
/// </summary>
internal sealed class EntityReuse(SelectionPath[] producerPaths, string[] responseNames)
{
    /// <summary>
    /// Gets the result paths of entities that already hold every field the lookup selects.
    /// </summary>
    public ReadOnlySpan<SelectionPath> ProducerPaths => producerPaths;

    /// <summary>
    /// Gets the response names of the leaf fields the lookup selects.
    /// </summary>
    public ReadOnlySpan<string> ResponseNames => responseNames;
}
