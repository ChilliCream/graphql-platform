namespace HotChocolate.Types.Analyzers;

public class GatewayInfo(string name, string typeName)
{
    public string Name { get; } = name;

    public string TypeName { get; } = typeName;

    public List<SubgraphInfo> Subgraphs { get; } = new();
}