using System.Collections.Generic;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a backlog for deferred work.
/// </summary>
internal interface IDeferredWorkScheduler
{
    /// <summary>
    /// Specifies if there was deferred work enqueued.
    /// </summary>
    bool HasResults { get; }

    /// <summary>
    /// Registers deferred work
    /// </summary>
    /// <param name="task"></param>
    /// <param name="deferId"></param>
    void Register(DeferredExecutionTask task, ResultData parentResult);

    void Register(DeferredExecutionTask task, uint patchId);

    void Complete(DeferredExecutionTaskResult result);

    IAsyncEnumerable<IQueryResult> CreateResultStream(IQueryResult initialResult);
}

internal readonly struct DeferredExecutionTaskResult
{
    public DeferredExecutionTaskResult(
        uint taskId,
        uint parentTaskId,
        IQueryResultBuilder? result = null)
    {
        TaskId = taskId;
        ParentTaskId = parentTaskId;
        Result = result;
    }

    public uint TaskId { get; }

    public  uint ParentTaskId { get; }

    public IQueryResultBuilder? Result { get; }
}
