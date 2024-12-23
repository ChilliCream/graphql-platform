using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeInputFieldCompletionContext(
    DirectiveCollection directives,
    ICompositeType type)
{
    public DirectiveCollection Directives { get; } = directives;

    public ICompositeType Type { get; } = type;
}
