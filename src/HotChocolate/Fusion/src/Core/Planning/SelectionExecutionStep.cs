using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal class SelectionExecutionStep : IExecutionStep
{
    public SelectionExecutionStep(
        string schemaNameName,
        ObjectType selectionSetType,
        ISelection? parentSelection)
    {
        SelectionSetType = selectionSetType;
        ParentSelection = parentSelection;
        SchemaName = schemaNameName;
    }

    public string SchemaName { get; }

    /// <summary>
    /// The type name of the root selection set of this execution step.
    /// If <see cref="ParentSelection"/> is null then the selection set is the
    /// operation root selection set, otherwise its the selection set resolved
    /// by using the <see cref="ParentSelection"/>.
    /// </summary>
    public ObjectType SelectionSetType { get; }

    public ISelection? ParentSelection { get; }

    public ResolverDefinition? Resolver { get; set; }

    public List<RootSelection> RootSelections { get; } = new();

    public HashSet<ISelection> AllSelections { get; } = new();

    public HashSet<ISelectionSet> AllSelectionSets { get; } = new();

    /// <summary>
    /// Gets the execution steps this execution step is depending on.
    /// </summary>
    public HashSet<IExecutionStep> DependsOn { get; } = new();

    /// <summary>
    /// Gets a map for this execution task from the variable name
    /// to the internal state key.
    /// </summary>
    public Dictionary<string, string> Variables { get; } = new();

    /// <summary>
    /// The variable requirements by this task.
    /// </summary>
    public HashSet<string> Requires { get; } = new();
}
