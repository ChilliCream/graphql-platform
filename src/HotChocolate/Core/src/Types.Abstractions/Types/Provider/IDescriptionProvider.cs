#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// A type system member that has a description.
/// </summary>
public interface IDescriptionProvider : ITypeSystemMember
{
    /// <summary>
    /// Gets the description of the <see cref="ITypeSystemMember"/>.
    /// </summary>
    string? Description { get; }
}
