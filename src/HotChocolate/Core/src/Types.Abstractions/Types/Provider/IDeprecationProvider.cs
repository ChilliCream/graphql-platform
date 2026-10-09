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
    /// This is <c>true</c> if a <see cref="DeprecationReason"/> is present.
    /// </summary>
    [MemberNotNullWhen(true, nameof(DeprecationReason))]
    bool IsDeprecated => DeprecationReason is not null;

    /// <summary>
    /// Defines if this <see cref="ITypeSystemMember"/> is deprecated without
    /// a specific deprecation reason being provided.
    /// </summary>
    bool HasDefaultDeprecationReason
        => string.Equals(
            DeprecationReason,
            DirectiveNames.Deprecated.Arguments.DefaultReason,
            StringComparison.Ordinal);

    /// <summary>
    /// Gets the deprecation reason of this <see cref="ITypeSystemMember"/>,
    /// or <c>null</c> if this member is not deprecated.
    /// </summary>
    string? DeprecationReason { get; }
}
