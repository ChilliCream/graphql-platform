namespace HotChocolate.Fusion.Types;

public class SourceInterfaceField(
    string name,
    string schemaName,
    ICompositeType type)
    : ISourceMember
{
    public string Name { get; } = name;

    public string SchemaName { get; } = schemaName;

    public ICompositeType Type { get; } = type;
}
