namespace HotChocolate.Fusion.Composition;

public sealed class SubgraphInfo(string name, string path, string variableName)
{
    public string Name { get; } = name;

    public string Path { get; } = path;

    public string VariableName { get; } = variableName;

    public static SubgraphInfo Create<TProject>(string name, string variableName)
        where TProject : IProjectMetadata, new()
        => new(name, new TProject().ProjectPath, variableName);
}