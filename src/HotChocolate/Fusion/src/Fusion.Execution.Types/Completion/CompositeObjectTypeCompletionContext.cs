using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal readonly ref struct CompositeObjectTypeCompletionContext(
    FusionDirectiveCollection directives,
    FusionInterfaceTypeDefinitionCollection interfaces,
    SourceObjectTypeCollection sources,
    ImmutableArray<PolicyApplication> policyApplications,
    IFeatureCollection features)
{
    public FusionDirectiveCollection Directives { get; } = directives;

    public FusionInterfaceTypeDefinitionCollection Interfaces { get; } = interfaces;

    public SourceObjectTypeCollection Sources { get; } = sources;

    public ImmutableArray<PolicyApplication> PolicyApplications { get; } = policyApplications;

    public IFeatureCollection Features { get; } = features;
}
