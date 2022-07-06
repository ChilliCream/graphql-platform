using System.Collections.Generic;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a backlog for deferred work.
/// </summary>
internal interface IDeferredWorkScheduler
{
    bool HasResults { get; }

    void Register(DeferredExecutionTask task);

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
