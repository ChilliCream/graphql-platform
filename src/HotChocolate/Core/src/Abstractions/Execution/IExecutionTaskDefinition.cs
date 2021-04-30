namespace HotChocolate.Execution
{
    public interface IExecutionTaskDefinition
    {
        IAsyncExecutionTask Create(IExecutionTaskContext context);
    }
}
