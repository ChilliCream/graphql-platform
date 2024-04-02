using System.Collections.Immutable;
using HotChocolate.Fusion.Aspire;

namespace HotChocolate.Fusion.Composition;

public sealed record GatewayInfo(
    string Name,
    string Path,
    FusionOptions Options,
    ImmutableArray<SubgraphInfo> Subgraphs)
    : IResourceAnnotation
{
    public static GatewayInfo Create<TProject>(string name, FusionOptions options, params SubgraphInfo[] projects)
        where TProject : IProjectMetadata, new()
        => new(name, new TProject().ProjectPath, options, ImmutableArray<SubgraphInfo>.Empty.AddRange(projects));
}
