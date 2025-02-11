using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeObjectFieldCompletionContext(
    FusionComplexType declaringType,
    DirectiveCollection directives,
    ICompositeType type,
    SourceObjectFieldCollection sources)
{
    public FusionComplexType DeclaringType { get; } = declaringType;

    public DirectiveCollection Directives { get; } = directives;

    public ICompositeType Type { get; } = type;

    public SourceObjectFieldCollection Sources { get; } = sources;
}
