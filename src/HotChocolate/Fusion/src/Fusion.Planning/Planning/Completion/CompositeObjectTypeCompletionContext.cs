using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Completion;

internal ref struct CompositeObjectTypeCompletionContext(
    ICompositeSchemaContext context,
    IImmutableList<DirectiveNode> directives,
    IImmutableList<NamedTypeNode> interfaces)
{
    public ICompositeSchemaContext Context { get; } = context;

    public IImmutableList<DirectiveNode> Directives { get; } = directives;

    public IImmutableList<NamedTypeNode> Interfaces { get; } = interfaces;
}
