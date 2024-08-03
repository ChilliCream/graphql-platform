using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Completion;

internal ref struct CompositeInputFieldCompletionContext(
    ICompositeSchemaContext context,
    IImmutableList<DirectiveNode> directives,
    ITypeNode type)
{
    public ICompositeSchemaContext Context { get; } = context;

    public IImmutableList<DirectiveNode> Directives { get; } = directives;

    public ITypeNode Type { get; } = type;
}
