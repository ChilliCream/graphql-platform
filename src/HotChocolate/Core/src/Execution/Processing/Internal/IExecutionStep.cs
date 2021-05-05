namespace HotChocolate.Execution.Processing.Internal
{
    public interface IExecutionStep
    {
        bool IsAllowed(IExecutionTask task);
    }
}
