using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Tracks the enclosing plan scope for each incremental plan descriptor.
/// </summary>
/// <remarks>
/// Descriptors are registered in parent-before-child order so nested
/// descriptors can resolve their immediate enclosing scope.
/// </remarks>
internal sealed class PlanContextGraph
{
    private readonly Dictionary<IncrementalPlanDescriptor, ParentPlanContext> _enclosingContextByDescriptor = [];
    private ParentPlanContext _rootContext;

    private PlanContextGraph(ParentPlanContext rootContext)
    {
        _rootContext = rootContext;
    }

    /// <summary>
    /// Creates a new <see cref="PlanContextGraph"/> seeded with the root plan
    /// scope.
    /// </summary>
    /// <param name="rootSteps">
    /// The root plan's current list of plan steps.
    /// </param>
    /// <param name="rootSelectionSetIndex">
    /// The selection set index associated with the root plan's internal
    /// operation.
    /// </param>
    /// <param name="rootInternalOperation">
    /// The planner's internal operation for the root plan scope. For an
    /// operation with <c>@defer</c> this is the stripped main operation.
    /// </param>
    public static PlanContextGraph Create(
        ImmutableList<PlanStep> rootSteps,
        ISelectionSetIndex rootSelectionSetIndex,
        OperationDefinitionNode rootInternalOperation)
    {
        ArgumentNullException.ThrowIfNull(rootSteps);
        ArgumentNullException.ThrowIfNull(rootSelectionSetIndex);
        ArgumentNullException.ThrowIfNull(rootInternalOperation);

        var rootContext = new ParentPlanContext(
            rootSteps,
            rootSelectionSetIndex,
            rootInternalOperation,
            ParentScope.Root);

        return new PlanContextGraph(rootContext);
    }

    /// <summary>
    /// Gets the current root plan scope's step list. Reflects any updates
    /// applied via <see cref="UpdateRootSteps(ImmutableList{PlanStep})"/>.
    /// </summary>
    public ImmutableList<PlanStep> RootSteps => _rootContext.ParentSteps;

    /// <summary>
    /// Gets the current root plan scope's operation definition.
    /// </summary>
    public OperationDefinitionNode RootInternalOperation => _rootContext.ParentInternalOperation;

    /// <summary>
    /// Returns the immediate enclosing <see cref="ParentPlanContext"/> for the
    /// given incremental plan descriptor.
    /// </summary>
    /// <param name="descriptor">The descriptor whose enclosing context is queried.</param>
    public ParentPlanContext GetParentContext(IncrementalPlanDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (descriptor.Parent is null)
        {
            return _rootContext;
        }

        if (!_enclosingContextByDescriptor.TryGetValue(descriptor.Parent, out var parentContext))
        {
            throw new InvalidOperationException(
                "The enclosing deferred incremental plan has not been registered with the defer context graph. "
                + "Parent incremental plans must be planned before their nested children.");
        }

        return parentContext;
    }

    /// <summary>
    /// Updates the root plan scope's step list.
    /// </summary>
    /// <param name="updatedRootSteps">The new root plan step list.</param>
    public void UpdateRootSteps(ImmutableList<PlanStep> updatedRootSteps)
    {
        ArgumentNullException.ThrowIfNull(updatedRootSteps);

        _rootContext = _rootContext with { ParentSteps = updatedRootSteps };
    }

    /// <summary>
    /// Updates the root plan scope's operation definition.
    /// </summary>
    /// <param name="updatedRootInternalOperation">The new root internal operation.</param>
    public void UpdateRootInternalOperation(OperationDefinitionNode updatedRootInternalOperation)
    {
        ArgumentNullException.ThrowIfNull(updatedRootInternalOperation);

        _rootContext = _rootContext with { ParentInternalOperation = updatedRootInternalOperation };
    }

    /// <summary>
    /// Registers an incremental plan scope for nested incremental plans.
    /// </summary>
    /// <param name="descriptor">The incremental plan descriptor that was just planned.</param>
    /// <param name="steps">The incremental plan's final plan steps.</param>
    /// <param name="selectionSetIndex">The selection set index used for the incremental plan.</param>
    /// <param name="internalOperation">The incremental plan's internal operation.</param>
    public void RegisterDeferContext(
        IncrementalPlanDescriptor descriptor,
        ImmutableList<PlanStep> steps,
        ISelectionSetIndex selectionSetIndex,
        OperationDefinitionNode internalOperation)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(selectionSetIndex);
        ArgumentNullException.ThrowIfNull(internalOperation);

        var context = new ParentPlanContext(
            steps,
            selectionSetIndex,
            internalOperation,
            ParentScope.EnclosingIncrementalPlan,
            OwnerDescriptor: descriptor);

        _enclosingContextByDescriptor[descriptor] = context;
    }

    /// <summary>
    /// Returns the next enclosing scope above <paramref name="scope"/>, or
    /// <see langword="null"/> when <paramref name="scope"/> is the root.
    /// The walker in <see cref="OperationPlanner.ApplyDeferRequirementsToParent"/>
    /// calls this to escalate a requirement that the current scope cannot
    /// satisfy (same-subgraph inline and cross-subgraph promote both failed).
    /// Lookups go through the graph each call so the walker always observes
    /// the current state of each scope, including mutations applied by
    /// sibling or nested descriptors that were processed in between.
    /// </summary>
    public ParentPlanContext? GetEnclosingScope(ParentPlanContext scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (scope.Kind == ParentScope.Root)
        {
            return null;
        }

        var ownerDescriptor = scope.OwnerDescriptor
            ?? throw new InvalidOperationException(
                "An EnclosingIncrementalPlan-kind scope must carry an OwnerDescriptor.");

        if (ownerDescriptor.Parent is null)
        {
            return _rootContext;
        }

        if (!_enclosingContextByDescriptor.TryGetValue(ownerDescriptor.Parent, out var next))
        {
            throw new InvalidOperationException(
                "The parent deferred incremental plan has not been registered with the defer context graph.");
        }

        return next;
    }

    /// <summary>
    /// Returns the registered step list for a descriptor, reflecting any
    /// parent-scope-style mutations applied after the initial registration
    /// (for example a nested inner defer promoting a step into its outer
    /// enclosing scope).
    /// </summary>
    public ImmutableList<PlanStep> GetRegisteredSteps(IncrementalPlanDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!_enclosingContextByDescriptor.TryGetValue(descriptor, out var context))
        {
            throw new InvalidOperationException(
                "The deferred incremental plan has not been registered with the defer context graph.");
        }

        return context.ParentSteps;
    }

    /// <summary>
    /// Returns the registered operation definition for a descriptor.
    /// </summary>
    public OperationDefinitionNode GetRegisteredInternalOperation(IncrementalPlanDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!_enclosingContextByDescriptor.TryGetValue(descriptor, out var context))
        {
            throw new InvalidOperationException(
                "The deferred incremental plan has not been registered with the defer context graph.");
        }

        return context.ParentInternalOperation;
    }

    /// <summary>
    /// Updates the registered enclosing-scope context of a descriptor after
    /// its plan absorbs an inner defer's requirement. Nested inner
    /// descriptors consult their immediate enclosing scope; when the outer
    /// scope gets a new parent-scope step appended for an inner's
    /// requirement, subsequent siblings of that inner must observe the
    /// update.
    /// </summary>
    public void UpdateDeferContext(
        IncrementalPlanDescriptor descriptor,
        ImmutableList<PlanStep> steps,
        OperationDefinitionNode? internalOperation = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(steps);

        if (!_enclosingContextByDescriptor.TryGetValue(descriptor, out var existing))
        {
            return;
        }

        _enclosingContextByDescriptor[descriptor] = existing with
        {
            ParentSteps = steps,
            ParentInternalOperation = internalOperation ?? existing.ParentInternalOperation
        };
    }
}
