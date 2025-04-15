#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public interface IDirectiveConfigurationProvider
{
    /// <summary>
    /// Specifies if any directives were registered.
    /// </summary>
    bool HasDirectives { get; }

    /// <summary>
    /// Gets the list of directives that are annotated to
    /// the implementing object.
    /// </summary>
    IList<DirectiveConfiguration> Directives { get; }

    /// <summary>
    /// Gets the list of directives that are annotated to
    /// the implementing object.
    /// </summary>
    IReadOnlyList<DirectiveConfiguration> GetDirectives();
}
