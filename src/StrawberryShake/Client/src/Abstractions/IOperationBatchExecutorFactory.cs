namespace StrawberryShake
{
    public interface IOperationBatchExecutorFactory
    {
        string Name { get; }

        IOperationBatchExecutor CreateBatchExecutor();
    }
}
