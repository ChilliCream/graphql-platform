using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal sealed class IntrospectionExecutionStep : IExecutionStep
{
    public IntrospectionExecutionStep(
        string schemaNameName,
        ObjectType selectionSetType,
        ISelection? parentSelection)
    {
        SelectionSetType = selectionSetType;
        ParentSelection = parentSelection;
        SubgraphName = schemaNameName;
    }

    public string SubgraphName { get; }

    public ObjectType SelectionSetType { get; }

    public ISelection? ParentSelection { get; }

    public ResolverDefinition? Resolver => null;

    public HashSet<IExecutionStep> DependsOn { get; } = new();
}
