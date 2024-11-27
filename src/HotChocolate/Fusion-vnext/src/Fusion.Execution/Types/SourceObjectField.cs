namespace HotChocolate.Fusion.Types;

public sealed class SourceObjectField(
    string name,
    string schemaName,
    FieldRequirements? requirements,
    ICompositeType type)
    : ISourceOutputField
{
    public string Name { get; } = name;

    public string SchemaName { get; } = schemaName;

    public FieldRequirements? Requirements { get; } = requirements;

    public ICompositeType Type { get; } = type;

    public int BaseCost => 1;
}

public interface ISourceOutputField : ISourceMember
{
    ICompositeType Type { get; }
}
