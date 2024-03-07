namespace HotChocolate.Types.Analyzers;

public class SubgraphInfo(string name, string typeName)
{
    public string Name { get; } = name;

    public string TypeName { get; } = typeName;
}