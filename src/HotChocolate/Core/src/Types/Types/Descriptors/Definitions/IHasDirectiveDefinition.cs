#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public interface IHasDirectiveDefinition
{
    /// <summary>
    /// Specifies if any directives were registered.
    /// </summary>
    bool HasDirectives { get; }

    /// <summary>
    /// Gets the list of directives that are annotated to
    /// the implementing object.
    /// </summary>
    IList<DirectiveDefinition> Directives { get; }

    /// <summary>
    /// Gets the list of directives that are annotated to
    /// the implementing object.
    /// </summary>
    IReadOnlyList<DirectiveDefinition> GetDirectives();
}
