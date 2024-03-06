namespace HotChocolate.Types.Analyzers;

public sealed class ProjectClass(string name, string typeName, string variableName) : ISyntaxInfo
{
    public string Name { get; } = name;

    public string TypeName { get; } = typeName;

    public string VariableName { get; } = variableName;
}

public sealed class GatewayClass(string name, string typeName, string variableName) : ISyntaxInfo
{
    public string Name { get; } = name;

    public string TypeName { get; } = typeName;

    public string VariableName { get; } = variableName;
}

public class GatewayInfo(string name, string typeName)
{
    public string Name { get; } = name;

    public string TypeName { get; } = typeName;

    public List<ProjectInfo> Projects { get; } = new();
}

public class ProjectInfo(string name, string typeName)
{
    public string Name { get; } = name;

    public string TypeName { get; } = typeName;
}

public interface ISyntaxInfo
{
    string Name { get; }
}