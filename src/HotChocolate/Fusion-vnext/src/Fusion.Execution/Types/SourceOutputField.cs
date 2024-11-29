namespace HotChocolate.Fusion.Types;

public sealed class SourceOutputField(
    string name,
    string schemaName,
    FieldRequirements? requirements,
    ICompositeType type)
    : ISourceMember
{
    public string Name { get; } = name;

    public string SchemaName { get; } = schemaName;

    public FieldRequirements? Requirements { get; } = requirements;

    public ICompositeType Type { get; } = type;

    public int BaseCost => 1;
}
