namespace HotChocolate.Fusion.Composition.Analyzers.Models;

public class GatewayInfo(string name, string variableName, string typeName)
{
    public string Name { get; } = name;

    public string VariableName { get; } = variableName;

    public string TypeName { get; } = typeName;

    public List<SubgraphInfo> Subgraphs { get; } = new();
}
