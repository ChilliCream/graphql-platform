
#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// A type system member that can be deprecated.
/// </summary>
public interface IDeprecationProvider : ITypeSystemMember
{
    /// <summary>
    /// Defines if this <see cref="ITypeSystemMember"/> is deprecated.
    /// </summary>
    bool IsDeprecated { get; }

    /// <summary>
    /// Gets the deprecation reason of this <see cref="ITypeSystemMember"/>.
    /// </summary>
    string? DeprecationReason { get; }
}
