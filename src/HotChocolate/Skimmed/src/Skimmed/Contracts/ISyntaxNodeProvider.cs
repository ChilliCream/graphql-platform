using HotChocolate.Language;

namespace HotChocolate.Skimmed;

/// <summary>
/// A type system member that can be converted to an <see cref="ISyntaxNode"/>.
/// </summary>
public interface ISyntaxNodeProvider : ITypeSystemMemberDefinition
{
    /// <summary>
    /// Creates an <see cref="ISyntaxNode"/> from an <see cref="ITypeSystemMemberDefinition"/>.
    /// </summary>
    ISyntaxNode ToSyntaxNode();
}
