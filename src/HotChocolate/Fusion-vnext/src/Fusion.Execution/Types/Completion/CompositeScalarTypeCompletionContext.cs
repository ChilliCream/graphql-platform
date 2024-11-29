using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeScalarTypeCompletionContext(
    DirectiveCollection directives)
{
    public DirectiveCollection Directives { get; } = directives;
}
