namespace HotChocolate.Execution
{
    public interface IExecutionTaskDefinition
    {
        IExecutionTask Create(IExecutionTaskContext context);
    }
}
