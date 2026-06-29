using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal readonly ref struct CompositeOutputFieldCompletionContext(
    FusionComplexTypeDefinition declaringType,
    FusionDirectiveCollection directives,
    IOutputType type,
    SourceObjectFieldCollection sources,
    ImmutableArray<PolicyApplication> policyApplications,
    IFeatureCollection features)
{
    public FusionComplexTypeDefinition DeclaringType { get; } = declaringType;

    public FusionDirectiveCollection Directives { get; } = directives;

    public IOutputType Type { get; } = type;

    public SourceObjectFieldCollection Sources { get; } = sources;

    public ImmutableArray<PolicyApplication> PolicyApplications { get; } = policyApplications;

    public IFeatureCollection Features { get; } = features;
}
