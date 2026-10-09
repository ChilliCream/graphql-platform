using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// One operation planned against one composed schema with a reused planner.
/// </summary>
internal sealed class PlanningSuiteCase
{
    private const string Id = "123456789101112";

    private readonly OperationPlanner _planner;
    private readonly OperationDefinitionNode _operation;

    public PlanningSuiteCase(OperationPlanner planner, OperationDefinitionNode operation)
    {
        _planner = planner;
        _operation = operation;
        ExpandedNodes = Plan().ExpandedNodes;
    }

    /// <summary>
    /// Gets the number of nodes the planner expanded to plan the operation.
    /// </summary>
    public int ExpandedNodes { get; }

    /// <summary>
    /// Plans the operation.
    /// </summary>
    public OperationPlan Plan() => _planner.CreatePlan(Id, Id, Id, _operation);
}
