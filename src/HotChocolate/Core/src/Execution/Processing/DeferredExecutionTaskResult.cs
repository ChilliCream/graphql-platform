namespace HotChocolate.Execution.Processing;

internal readonly struct DeferredExecutionTaskResult
{
    public DeferredExecutionTaskResult(
        uint taskId,
        uint parentTaskId,
        IOperationResult? result = null)
    {
        TaskId = taskId;
        ParentTaskId = parentTaskId;
        Result = result;
    }

    public uint TaskId { get; }

    public  uint ParentTaskId { get; }

    public IOperationResult? Result { get; }
}
