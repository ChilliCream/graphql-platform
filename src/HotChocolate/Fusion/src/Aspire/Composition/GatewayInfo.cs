using System.Collections.Immutable;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire;

namespace HotChocolate.Fusion.Composition;

public sealed record GatewayInfo(
    string Name,
    string Path,
    FusionCompositionOptions CompositionOptions,
    ImmutableArray<SubgraphInfo> Subgraphs)
    : IResourceAnnotation
{
    public static GatewayInfo Create<TProject>(string name, FusionCompositionOptions compositionOptions, params SubgraphInfo[] projects)
        where TProject : IProjectMetadata, new()
        => new(name, new TProject().ProjectPath, compositionOptions, ImmutableArray<SubgraphInfo>.Empty.AddRange(projects));
}
