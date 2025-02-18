using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeInputFieldCompletionContext(
    FusionDirectiveCollection directives,
    IType type)
{
    public FusionDirectiveCollection Directives { get; } = directives;

    public IType Type { get; } = type;
}
