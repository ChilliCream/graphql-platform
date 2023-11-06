namespace HotChocolate.Execution.Processing;

internal readonly struct DeferredExecutionTaskResult
{
    public DeferredExecutionTaskResult(
        uint taskId,
        uint parentTaskId,
        IQueryResult? result = null)
    {
        TaskId = taskId;
        ParentTaskId = parentTaskId;
        Result = result;
    }

    public uint TaskId { get; }

    public  uint ParentTaskId { get; }

    public IQueryResult? Result { get; }
}
