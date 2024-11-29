namespace HotChocolate.Fusion.Metadata;

internal sealed class SubgraphInfo
{
    public SubgraphInfo(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public List<string> Entities { get; } = [];
}
