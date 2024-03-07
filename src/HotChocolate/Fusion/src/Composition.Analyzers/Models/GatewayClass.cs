namespace HotChocolate.Types.Analyzers;

public sealed class GatewayClass(string name, string typeName, string variableName) : ISyntaxInfo
{
    public string Name { get; } = name;

    public string TypeName { get; } = typeName;

    public string VariableName { get; } = variableName;
}