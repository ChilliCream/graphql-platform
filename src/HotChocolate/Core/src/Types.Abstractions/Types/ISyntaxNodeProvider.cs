using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// A type system member that can be converted to an <see cref="ISyntaxNode"/>.
/// </summary>
public interface ISyntaxNodeProvider : ITypeSystemMemberDefinition
{
    /// <summary>
    /// Creates an <see cref="ISyntaxNode"/> from a type system meber.
    /// </summary>
    ISyntaxNode ToSyntaxNode();
}
