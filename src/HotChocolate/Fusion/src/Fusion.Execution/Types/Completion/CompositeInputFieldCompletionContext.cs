using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal readonly ref struct CompositeInputFieldCompletionContext(
    ITypeSystemMember declaringMember,
    FusionDirectiveCollection directives,
    IInputType type,
    IFeatureCollection features)
{
    public ITypeSystemMember DeclaringMember { get; } = declaringMember;

    public FusionDirectiveCollection Directives { get; } = directives;

    public IInputType Type { get; } = type;

    public IFeatureCollection Features { get; } = features;
}
