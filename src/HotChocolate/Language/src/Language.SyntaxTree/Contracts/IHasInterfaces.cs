using System.Collections.Generic;

namespace HotChocolate.Language;

public interface IHasInterfaces
{
    /// <summary>
    /// Gets the directives of a syntax node.
    /// </summary>
    IReadOnlyList<NamedTypeNode> Interfaces { get; }
}
