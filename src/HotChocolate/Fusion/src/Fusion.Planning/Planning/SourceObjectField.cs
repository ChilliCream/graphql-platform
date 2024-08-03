using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

public class SourceObjectField(
    string name,
    string schemaName,
    ImmutableArray<Requirement> requirements,
    ICompositeType type)
    : ISourceField
{
    public string Name { get; } = name;

    public string SchemaName { get; } = schemaName;

    public ImmutableArray<Requirement> Requirements { get; } = requirements;

    public ICompositeType Type { get; } = type;

    internal void Complete()
    {
        
    }
}
