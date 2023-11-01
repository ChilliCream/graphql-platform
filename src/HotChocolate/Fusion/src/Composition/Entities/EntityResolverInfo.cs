using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal sealed class EntityResolverInfo(
    string entityName, 
    string originalName,
    ResolverKind kind, 
    OutputField field,
    EntitySourceField sourceField, 
    IReadOnlyList<EntitySourceArgument> sourceArguments)
{
    public string EntityName { get; } = entityName;
    
    public string OriginalName { get; } = originalName;

    public ResolverKind Kind { get; } = kind;
    
    public OutputField Field { get; } = field;

    public EntitySourceField SourceField { get; } = sourceField;

    public IReadOnlyList<EntitySourceArgument> SourceArguments { get; } = sourceArguments;
}