using System.Diagnostics.CodeAnalysis;

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
    /// This is <c>true</c> if and only if <see cref="DeprecationReason"/> is a non-empty string.
    /// </summary>
    [MemberNotNullWhen(true, nameof(DeprecationReason))]
    bool IsDeprecated { get; }

    /// <summary>
    /// Gets the deprecation reason of this <see cref="ITypeSystemMember"/>,
    /// or <c>null</c> or an empty string if this member is not deprecated.
    /// </summary>
    string? DeprecationReason { get; }
}
