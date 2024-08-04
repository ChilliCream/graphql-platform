namespace HotChocolate.Fusion.Planning;

public class SourceObjectField(
    string name,
    string schemaName,
    FieldRequirements? requirements,
    ICompositeType type)
    : ISourceField
{
    public string Name { get; } = name;

    public string SchemaName { get; } = schemaName;

    public FieldRequirements? Requirements { get; } = requirements;

    public ICompositeType Type { get; } = type;
}
