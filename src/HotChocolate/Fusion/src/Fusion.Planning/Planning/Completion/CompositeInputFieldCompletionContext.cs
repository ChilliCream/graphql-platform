using HotChocolate.Fusion.Planning.Collections;

namespace HotChocolate.Fusion.Planning.Completion;

internal ref struct CompositeInputFieldCompletionContext(
    DirectiveCollection directives,
    ICompositeType type)
{
    public DirectiveCollection Directives { get; } = directives;

    public ICompositeType Type { get; } = type;
}
