using HotChocolate.Language;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// A type system member that can be converted to an <see cref="ISyntaxNode"/>.
/// </summary>
public interface ISyntaxNodeProvider : ITypeSystemMember
{
    /// <summary>
    /// Creates an <see cref="ISyntaxNode"/> from a type system member.
    /// </summary>
    ISyntaxNode ToSyntaxNode();
}

/// <summary>
/// A type system member that can be converted to an <see cref="ISyntaxNode"/>.
/// </summary>
/// <typeparam name="TNode">The type of the syntax node.</typeparam>
public interface ISyntaxNodeProvider<out TNode> : ISyntaxNodeProvider
    where TNode : ISyntaxNode
{
    /// <summary>
    /// Creates an <see cref="ISyntaxNode"/> from a type system member.
    /// </summary>
    new TNode ToSyntaxNode();
}
