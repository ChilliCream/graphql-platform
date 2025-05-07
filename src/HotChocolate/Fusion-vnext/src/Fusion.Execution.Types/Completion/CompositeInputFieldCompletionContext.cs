using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeInputFieldCompletionContext(
    ITypeSystemMember declaringMember,
    FusionDirectiveCollection directives,
    IType type,
    IFeatureCollection features)
{
    public ITypeSystemMember DeclaringMember { get; } = declaringMember;

    public FusionDirectiveCollection Directives { get; } = directives;

    public IType Type { get; } = type;

    public IFeatureCollection Features { get; } = features;
}
