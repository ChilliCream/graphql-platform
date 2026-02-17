using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// An immutable backlog of work items paired with cost tracking.
/// Every push and pop keeps the cost in sync with the stack,
/// so branch scoring stays O(1) per transition.
/// </summary>
internal readonly struct Backlog(ImmutableStack<WorkItem> items, BacklogCost cost)
{
    private readonly ImmutableStack<WorkItem> _items = items;

    public static Backlog Empty { get; } = new([], BacklogCost.Empty);

    /// <summary>
    /// The cost tracking state for all items in this backlog.
    /// </summary>
    public BacklogCost Cost { get; } = cost;

    /// <summary>
    /// Whether this backlog has no work items left.
    /// </summary>
    public bool IsEmpty => _items.IsEmpty;

    /// <summary>
    /// Returns the next work item without removing it.
    /// </summary>
    public WorkItem Peek() => _items.Peek();

    /// <summary>
    /// Pushes a work item and updates cost tracking.
    /// </summary>
    public Backlog Push(WorkItem workItem)
    {
        var cost = PlannerCostEstimator.AddWorkItemCost(Cost, workItem);
        return new Backlog(_items.Push(workItem), cost);
    }

    /// <summary>
    /// Pops the next work item and updates cost tracking.
    /// </summary>
    public Backlog Pop(out WorkItem workItem)
    {
        var items = _items.Pop(out workItem);
        var cost = PlannerCostEstimator.RemoveWorkItemCost(Cost, workItem);
        return new Backlog(items, cost);
    }

    /// <summary>
    /// Pushes work items for unresolvable selection sets that need
    /// their own operation steps on other schemas.
    /// </summary>
    public Backlog PushUnresolvable(
        ImmutableStack<SelectionSet> unresolvable,
        string fromSchema,
        int parentDepth)
    {
        if (unresolvable.IsEmpty)
        {
            return this;
        }

        var backlog = this;

        foreach (var selectionSet in unresolvable.Reverse())
        {
            var workItem = new OperationWorkItem(
                selectionSet.Path.IsRoot
                    ? OperationWorkItemKind.Root
                    : OperationWorkItemKind.Lookup,
                selectionSet,
                FromSchema: fromSchema)
            {
                ParentDepth = parentDepth
            };
            backlog = backlog.Push(workItem);
        }

        return backlog;
    }

    /// <summary>
    /// Pushes work items for fields that have requirements
    /// which may need lookup steps to satisfy.
    /// </summary>
    public Backlog PushRequirements(
        ImmutableStack<FieldSelection> fieldsWithRequirements,
        int stepId,
        int parentDepth)
    {
        if (fieldsWithRequirements.IsEmpty)
        {
            return this;
        }

        var backlog = this;

        foreach (var selection in fieldsWithRequirements.Reverse())
        {
            var workItem = new FieldRequirementWorkItem(selection, stepId)
            {
                ParentDepth = parentDepth
            };
            backlog = backlog.Push(workItem);
        }

        return backlog;
    }
}
