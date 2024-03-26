using HotChocolate.Fusion.Aspire;

namespace HotChocolate.Fusion.Composition;

public sealed class GatewayInfo(string name, string path, FusionOptions options, IReadOnlyList<SubgraphInfo> subgraphs)
{
    public string Name { get; } = name;

    public string Path { get; } = path;

    public FusionOptions Options { get; } = options;

    public IReadOnlyList<SubgraphInfo> Subgraphs { get; } = subgraphs;

    public static GatewayInfo Create<TProject>(string name, FusionOptions options, params SubgraphInfo[] projects)
        where TProject : IProjectMetadata, new()
        => new(name, new TProject().ProjectPath, options, projects);
}
