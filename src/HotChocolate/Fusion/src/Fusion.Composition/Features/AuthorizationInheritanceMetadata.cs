namespace HotChocolate.Fusion.Features;

/// <summary>
/// The members of the merged schema that newly require authorization because of interface
/// inheritance, although none of their source schemas annotated them directly.
/// </summary>
internal sealed class AuthorizationInheritanceMetadata
{
    public List<InheritedAuthorization> Entries { get; } = [];
}
