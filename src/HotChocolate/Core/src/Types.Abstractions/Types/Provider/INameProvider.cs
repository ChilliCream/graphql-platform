#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// A type system member that has a name.
/// </summary>
public interface INameProvider : ITypeSystemMember
{
    /// <summary>
    /// Gets the name of the type system member.
    /// </summary>
    /// <value>
    /// The name of the type system member.
    /// </value>
    string Name { get; }
}
