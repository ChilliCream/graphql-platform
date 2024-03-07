namespace HotChocolate.Fusion.Composition.Analyzers.Models;

public sealed class SubgraphClass(string name, string typeName, string variableName) : ISyntaxInfo
{
    public string Name { get; } = name;

    public string TypeName { get; } = typeName;

    public string VariableName { get; } = variableName;
}