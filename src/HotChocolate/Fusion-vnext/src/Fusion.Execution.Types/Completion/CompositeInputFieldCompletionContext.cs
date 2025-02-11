using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeInputFieldCompletionContext(
    DirectiveCollection directives,
    ICompositeType type)
{
    public DirectiveCollection Directives { get; } = directives;

    public IType Type { get; } = type;
}
