using System.Collections.Immutable;

namespace HotChocolate.Fusion.Features;

/// <summary>
/// The members of the merged schema that newly require a policy because of interface
/// inheritance, although none of their source schemas annotated them directly.
/// </summary>
internal sealed class PolicyInheritanceMetadata(
    ImmutableArray<InheritedPolicy> entries)
{
    public ImmutableArray<InheritedPolicy> Entries { get; } = entries;
}
