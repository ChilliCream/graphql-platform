namespace HotChocolate.Fusion.Planning;

public class SourceInterfaceField(
    string name,
    string schemaName,
    ICompositeType type)
    : ISourceField
{
    public string Name { get; } = name;

    public string SchemaName { get; } = schemaName;

    public ICompositeType Type { get; } = type;
}
