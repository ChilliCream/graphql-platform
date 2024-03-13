namespace HotChocolate.Fusion.Composition;

public sealed class GatewayInfo(string name, string path, IReadOnlyList<SubgraphInfo> subgraphs)
{
    public string Name { get; } = name;

    public string Path { get; } = path;

    public IReadOnlyList<SubgraphInfo> Subgraphs { get; } = subgraphs;

    public static GatewayInfo Create<TProject>(string name, params SubgraphInfo[] projects)
        where TProject : IProjectMetadata, new()
        => new(name, new TProject().ProjectPath, projects);
}