using HotChocolate.Fusion.Planning.Collections;

namespace HotChocolate.Fusion.Planning.Completion;

internal ref struct CompositeScalarTypeCompletionContext(
    DirectiveCollection directives)
{
    public DirectiveCollection Directives { get; } = directives;
}
