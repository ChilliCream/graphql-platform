using Aspire.Hosting;

namespace HotChocolate.Fusion.Composition;

public sealed class SubgraphInfo(string name, string path)
{
    public string Name { get; } = name;

    public string Path { get; } = path;

    public static SubgraphInfo Create<TProject>(string name)
        where TProject : IProjectMetadata, new()
        => new(name, new TProject().ProjectPath);
}
