namespace HotChocolate.Fusion.Composition.Analyzers.Models;

public class SubgraphInfo(string name, string variableName, string typeName)
{
    public string Name { get; } = name;

    public string VariableName { get; } = variableName;

    public string TypeName { get; } = typeName;
}
