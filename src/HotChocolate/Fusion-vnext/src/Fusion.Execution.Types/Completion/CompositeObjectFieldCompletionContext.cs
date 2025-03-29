using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeObjectFieldCompletionContext(
    FusionComplexTypeDefinition declaringType,
    FusionDirectiveCollection directives,
    IType type,
    SourceObjectFieldCollection sources)
{
    public FusionComplexTypeDefinition DeclaringType { get; } = declaringType;

    public FusionDirectiveCollection Directives { get; } = directives;

    public IType Type { get; } = type;

    public SourceObjectFieldCollection Sources { get; } = sources;
}
