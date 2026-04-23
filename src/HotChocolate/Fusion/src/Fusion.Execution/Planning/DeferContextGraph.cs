using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Tracks the parent plan scope for every deferred sub-plan during planning.
/// Built before defer planning begins and refined as each sub-plan is
/// planned so that nested defers can resolve against their immediate
/// enclosing sub-plan rather than the root.
/// </summary>
/// <remarks>
/// The immediate enclosing relationship is taken from
/// <see cref="DeferSubPlanDescriptor.Parent"/>, which the
/// <see cref="DeferOperationRewriter"/> populates during the split phase by
/// walking each set member's <see cref="Execution.Nodes.DeferUsage.Parent"/>
/// chain. Because the rewriter emits descriptors in an order where every
/// parent precedes its children, each nested descriptor's enclosing scope is
/// already registered by the time it is queried here.
/// </remarks>
internal sealed class DeferContextGraph
{
    private readonly Dictionary<DeferSubPlanDescriptor, DeferParentContext> _enclosingContextByDescriptor = [];
    private DeferParentContext _rootContext;

    private DeferContextGraph(DeferParentContext rootContext)
    {
        _rootContext = rootContext;
    }

    /// <summary>
    /// Creates a new <see cref="DeferContextGraph"/> seeded with the root
    /// plan scope state that every top-level deferred sub-plan resolves
    /// against.
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
    public static DeferContextGraph Create(
        ImmutableList<PlanStep> rootSteps,
        ISelectionSetIndex rootSelectionSetIndex,
        OperationDefinitionNode rootInternalOperation)
    {
        ArgumentNullException.ThrowIfNull(rootSteps);
        ArgumentNullException.ThrowIfNull(rootSelectionSetIndex);
        ArgumentNullException.ThrowIfNull(rootInternalOperation);

        var rootContext = new DeferParentContext(
            rootSteps,
            rootSelectionSetIndex,
            rootInternalOperation,
            ParentScope.Root);

        return new DeferContextGraph(rootContext);
    }

    /// <summary>
    /// Gets the current root plan scope's step list. Reflects any updates
    /// applied via <see cref="UpdateRootSteps(ImmutableList{PlanStep})"/>.
    /// </summary>
    public ImmutableList<PlanStep> RootSteps => _rootContext.ParentSteps;

    /// <summary>
    /// Gets the current root plan scope's internal operation definition.
    /// Reflects any updates applied via
    /// <see cref="UpdateRootInternalOperation(OperationDefinitionNode)"/>.
    /// The routing pass must run BEFORE the root operation is compiled so
    /// that compile consumes this updated internal operation (which carries
    /// every field absorbed from a defer sub-plan's self-fetch). The
    /// mutation itself lives inside the defer routing pass because a
    /// sub-plan's required key is only known after its self-fetch has
    /// planned, which cannot happen until the main operation is known.
    /// </summary>
    public OperationDefinitionNode RootInternalOperation => _rootContext.ParentInternalOperation;

    /// <summary>
    /// Returns the immediate enclosing <see cref="DeferParentContext"/> for
    /// the given sub-plan descriptor. A top-level defer resolves against the
    /// root scope, a nested defer resolves against its enclosing sub-plan's
    /// context as registered via
    /// <see cref="RegisterDeferContext(DeferSubPlanDescriptor, ImmutableList{PlanStep}, ISelectionSetIndex, OperationDefinitionNode)"/>.
    /// </summary>
    /// <param name="descriptor">The descriptor whose enclosing context is queried.</param>
    public DeferParentContext GetParentContext(DeferSubPlanDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (descriptor.Parent is null)
        {
            return _rootContext;
        }

        if (!_enclosingContextByDescriptor.TryGetValue(descriptor.Parent, out var parentContext))
        {
            throw new InvalidOperationException(
                "The enclosing defer sub-plan has not been registered with the defer context graph. "
                + "Parent sub-plans must be planned before their nested children.");
        }

        return parentContext;
    }

    /// <summary>
    /// Updates the root plan scope's step list. Called after each top-level
    /// deferred sub-plan is planned so that subsequent top-level sub-plans
    /// see the requirement-inlining transformations the prior sub-plan
    /// applied.
    /// </summary>
    /// <param name="updatedRootSteps">The new root plan step list.</param>
    public void UpdateRootSteps(ImmutableList<PlanStep> updatedRootSteps)
    {
        ArgumentNullException.ThrowIfNull(updatedRootSteps);

        _rootContext = _rootContext with { ParentSteps = updatedRootSteps };
    }

    /// <summary>
    /// Updates the root plan scope's internal operation definition after a
    /// sub-plan's routing inlined a required field. Subsequent sub-plans
    /// read the updated operation through <see cref="GetParentContext"/>.
    /// The defer routing pass must run before the root operation is
    /// compiled so that compile consumes the fully-absorbed internal op.
    /// </summary>
    /// <param name="updatedRootInternalOperation">The new root internal operation.</param>
    public void UpdateRootInternalOperation(OperationDefinitionNode updatedRootInternalOperation)
    {
        ArgumentNullException.ThrowIfNull(updatedRootInternalOperation);

        _rootContext = _rootContext with { ParentInternalOperation = updatedRootInternalOperation };
    }

    /// <summary>
    /// Registers the planning output of a deferred sub-plan so that nested
    /// sub-plans can resolve their enclosing scope against it.
    /// </summary>
    /// <param name="descriptor">The sub-plan descriptor that was just planned.</param>
    /// <param name="steps">The sub-plan's final plan steps.</param>
    /// <param name="selectionSetIndex">The selection set index used for the sub-plan.</param>
    /// <param name="internalOperation">The sub-plan's internal operation.</param>
    public void RegisterDeferContext(
        DeferSubPlanDescriptor descriptor,
        ImmutableList<PlanStep> steps,
        ISelectionSetIndex selectionSetIndex,
        OperationDefinitionNode internalOperation)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(selectionSetIndex);
        ArgumentNullException.ThrowIfNull(internalOperation);

        var context = new DeferParentContext(
            steps,
            selectionSetIndex,
            internalOperation,
            ParentScope.EnclosingDefer,
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
    public DeferParentContext? GetEnclosingScope(DeferParentContext scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (scope.Kind == ParentScope.Root)
        {
            return null;
        }

        var ownerDescriptor = scope.OwnerDescriptor
            ?? throw new InvalidOperationException(
                "An EnclosingDefer-kind scope must carry an OwnerDescriptor.");

        if (ownerDescriptor.Parent is null)
        {
            return _rootContext;
        }

        if (!_enclosingContextByDescriptor.TryGetValue(ownerDescriptor.Parent, out var next))
        {
            throw new InvalidOperationException(
                "The parent defer sub-plan has not been registered with the defer context graph.");
        }

        return next;
    }

    /// <summary>
    /// Returns the registered step list for a descriptor, reflecting any
    /// parent-scope-style mutations applied after the initial registration
    /// (for example a nested inner defer promoting a step into its outer
    /// enclosing scope).
    /// </summary>
    public ImmutableList<PlanStep> GetRegisteredSteps(DeferSubPlanDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!_enclosingContextByDescriptor.TryGetValue(descriptor, out var context))
        {
            throw new InvalidOperationException(
                "The deferred sub-plan has not been registered with the defer context graph.");
        }

        return context.ParentSteps;
    }

    /// <summary>
    /// Returns the registered internal operation for a descriptor, reflecting
    /// any parent-scope-style mutations applied after the initial registration
    /// (for example a nested inner defer inlining a field into the outer
    /// sub-plan's operation document). Symmetric with
    /// <see cref="GetRegisteredSteps"/>; keeps the execution-node build pass
    /// from reading a stale snapshot taken at routing time.
    /// </summary>
    public OperationDefinitionNode GetRegisteredInternalOperation(DeferSubPlanDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!_enclosingContextByDescriptor.TryGetValue(descriptor, out var context))
        {
            throw new InvalidOperationException(
                "The deferred sub-plan has not been registered with the defer context graph.");
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
        DeferSubPlanDescriptor descriptor,
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
