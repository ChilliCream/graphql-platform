using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeObjectFieldCompletionContext(
    FusionComplexType declaringType,
    DirectiveCollection directives,
    IType type,
    SourceObjectFieldCollection sources)
{
    public FusionComplexType DeclaringType { get; } = declaringType;

    public DirectiveCollection Directives { get; } = directives;

    public IType Type { get; } = type;

    public SourceObjectFieldCollection Sources { get; } = sources;
}
