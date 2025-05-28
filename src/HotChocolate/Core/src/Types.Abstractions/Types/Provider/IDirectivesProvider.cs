#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// A type system member that has directives.
/// </summary>
public interface IDirectivesProvider : ITypeSystemMember
{
    /// <summary>
    /// Gets the directives of the type system member.
    /// </summary>
    /// <value>
    /// The directives of the type system member.
    /// </value>
    IReadOnlyDirectiveCollection Directives { get; }
}
