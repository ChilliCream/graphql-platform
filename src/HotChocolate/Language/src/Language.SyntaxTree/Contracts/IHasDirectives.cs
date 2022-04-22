using System.Collections.Generic;

namespace HotChocolate.Language;

/// <summary>
/// Represents a syntax node that has directives.
/// </summary>
public interface IHasDirectives
{
    /// <summary>
    /// Gets the directives of a syntax node.
    /// </summary>
    IReadOnlyList<DirectiveNode> Directives { get; }
}
