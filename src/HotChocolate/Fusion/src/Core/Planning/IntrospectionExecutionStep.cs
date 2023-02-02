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
        SchemaName = schemaNameName;
    }

    public string SchemaName { get; }

    public ObjectType SelectionSetType { get; }

    public ISelection? ParentSelection { get; }

    public FetchDefinition? Resolver => null;

    public HashSet<IExecutionStep> DependsOn { get; } = new();
}
