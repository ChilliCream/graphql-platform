using System.Collections.Immutable;

namespace HotChocolate.Fusion.Features;

/// <summary>
/// The members of the merged schema that newly require authorization because of interface
/// inheritance, although none of their source schemas annotated them directly.
/// </summary>
internal sealed class AuthorizationInheritanceMetadata(
    ImmutableArray<InheritedAuthorization> entries)
{
    public ImmutableArray<InheritedAuthorization> Entries { get; } = entries;
}
