namespace StrawberryShake
{
    public interface IOperationExecutorFactory
    {
        IOperationExecutor CreateExecutor(string name);

        IOperationBatchExecutor CreateBatchExecutor(string name);

        IOperationStreamExecutor CreateStreamExecutor(string name);
    }
}
