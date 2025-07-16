using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal readonly ref struct CompositeObjectFieldCompletionContext(
    FusionComplexTypeDefinition declaringType,
    FusionDirectiveCollection directives,
    IOutputType type,
    SourceObjectFieldCollection sources,
    IFeatureCollection features)
{
    public FusionComplexTypeDefinition DeclaringType { get; } = declaringType;

    public FusionDirectiveCollection Directives { get; } = directives;

    public IOutputType Type { get; } = type;

    public SourceObjectFieldCollection Sources { get; } = sources;

    public IFeatureCollection Features { get; } = features;
}
