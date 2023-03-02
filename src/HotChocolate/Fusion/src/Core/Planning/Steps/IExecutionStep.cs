using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents a execution step within the execution plan while being in the planing phase.
/// After the planing phase execution steps are compiled into execution nodes.
/// </summary>
internal interface IExecutionStep
{
    /// <summary>
    /// Gets the sub-graph from which this execution step will fetch data.
    /// </summary>
    string SubGraphName { get; }

    /// <summary>
    /// Gets the declaring type of the root selection set of this execution step.
    /// </summary>
    ObjectType SelectionSetType { get; }

    /// <summary>
    /// Gets the parent selection.
    /// </summary>
    ISelection? ParentSelection { get; }

    /// <summary>
    /// Gets the resolver for this execution step.
    /// </summary>
    ResolverDefinition? Resolver { get; }

    /// <summary>
    /// Gets the execution steps this execution step is depending on.
    /// </summary>
    HashSet<IExecutionStep> DependsOn { get; }
}
