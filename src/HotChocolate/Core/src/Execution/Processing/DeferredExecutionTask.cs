using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a deprioritized part of the query that will be executed after
/// the main execution has finished.
/// </summary>
internal abstract class DeferredExecutionTask
{
    /// <summary>
    /// Initializes a new instance of <see cref="DeferredExecutionTask"/>.
    /// </summary>
    protected DeferredExecutionTask(IImmutableDictionary<string, object?> scopedContextData)
    {
        ScopedContextData = scopedContextData;
    }

    /// <summary>
    /// Gets the preserved scoped context from the parent resolver.
    /// </summary>
    public IImmutableDictionary<string, object?> ScopedContextData { get; }

    /// <summary>
    /// Creates a dispatcher for the deferred execution task.
    /// </summary>
    /// <param name="operationContextOwner">
    /// The operation context owner.
    /// </param>
    /// <param name="resultId">
    /// The internal result identifier.
    /// </param>
    /// <param name="patchId">
    /// The internal identifier of the object that the result will be patched into.
    /// </param>
    public TaskDispatcher CreateDispatcher(OperationContextOwner operationContextOwner, uint resultId, uint patchId)
    {
        // retrieve the task on which this task depends upon. We do this to ensure that the result
        // of this task is not delivered before the parent result is delivered.
        uint parentResultId = 0;
        if (ScopedContextData.TryGetValue(DeferredResultId, out var value) &&
            value is uint id)
        {
            parentResultId = id;
        }

        return new(this, operationContextOwner, resultId, parentResultId, patchId);
    }

    /// <summary>
    /// The task execution logic.
    /// </summary>
    /// <param name="operationContextOwner">
    /// The operation context owner.
    /// </param>
    /// <param name="resultId">
    /// The internal result identifier.
    /// </param>
    /// <param name="parentResultId">
    /// The parent result identifier.
    /// </param>
    /// <param name="patchId">
    /// The internal identifier of the object that the result will be patched into.
    /// </param>
    protected abstract Task ExecuteAsync(
        OperationContextOwner operationContextOwner,
        uint resultId,
        uint parentResultId,
        uint patchId);

    /// <summary>
    /// Wrapper which is used to dispatch the deferred execution task.
    /// </summary>
    /// <param name="task">
    /// Deferred execution task.
    /// </param>
    /// <param name="operationContextOwner">
    /// The operation context owner.
    /// </param>
    /// <param name="resultId">
    /// The internal result identifier.
    /// </param>
    /// <param name="parentResultId">
    /// The parent result identifier.
    /// </param>
    /// <param name="patchId">
    /// The internal identifier of the object that the result will be patched into.
    /// </param>
    public class TaskDispatcher(
        DeferredExecutionTask task,
        OperationContextOwner operationContextOwner,
        uint resultId,
        uint parentResultId,
        uint patchId)
    {
        private bool _isDispatched = false;

        /// <summary>
        /// Dispatches the deferred execution task.
        /// </summary>
        public void Dispatch()
        {
            if (_isDispatched)
                throw new InvalidOperationException("Task was already dispatched with current dispatcher.");

            _isDispatched = true;

            _ = Task.Factory.StartNew(
                () => task.ExecuteAsync(operationContextOwner, resultId, parentResultId, patchId),
                default,
                TaskCreationOptions.None,
                TaskScheduler.Default);
        }
    }
}
