using System.Collections.Immutable;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a deprioritized fragment of the query that will be executed after
/// the main execution has finished.
/// </summary>
internal sealed class DeferredFragment : DeferredExecutionTask
{
    /// <summary>
    /// Initializes a new instance of <see cref="DeferredFragment"/>.
    /// </summary>
    public DeferredFragment(
        IFragment fragment,
        string? label,
        Path path,
        object? parent,
        IImmutableDictionary<string, object?> scopedContextData)
        : base(scopedContextData)
    {
        Fragment = fragment;
        Label = label;
        Path = path;
        Parent = parent;
    }

    /// <summary>
    /// Gets the deferred fragment.
    /// </summary>
    public IFragment Fragment { get; }

    /// <summary>
    /// If this argument label has a value other than null, it will be passed
    /// on to the result of this defer directive. This label is intended to
    /// give client applications a way to identify to which fragment a deferred
    /// result belongs to.
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// Gets the result path into which this deferred fragment shall be patched.
    /// </summary>
    public Path Path { get; }

    /// <summary>
    /// Gets the parent / source value.
    /// </summary>
    public object? Parent { get; }

    protected override async Task ExecuteAsync(
        OperationContextOwner operationContextOwner,
        uint resultId,
        uint parentResultId,
        uint patchId)
    {
        try
        {
            var operationContext = operationContextOwner.OperationContext;
            var parentResult = operationContext.Result.RentObject(Fragment.SelectionSet.Selections.Count);

            parentResult.PatchPath = Path;

            EnqueueResolverTasks(
                operationContext,
                Fragment.SelectionSet,
                Parent,
                Path,
                // for the execution of this task we set the deferred task ID so that
                // child deferrals can lookup their dependency to this task.
                ScopedContextData.SetItem(DeferredResultId, resultId),
                parentResult);

            // start executing the deferred fragment.
            await operationContext.Scheduler.ExecuteAsync().ConfigureAwait(false);

            // we create the result but will not create the final result object yet.
            // We will leave the final creation to the deferred work scheduler so that the
            // has next property can be correctly set.
            var result =
                operationContext
                    .SetLabel(Label)
                    .SetPath(Path)
                    .SetData(parentResult)
                    .SetPatchId(patchId)
                    .BuildResult();

            // complete the task and provide the result
            operationContext.DeferredScheduler.Complete(new(resultId, parentResultId, result));
        }
        finally
        {
            operationContextOwner.Dispose();
        }
    }
}
