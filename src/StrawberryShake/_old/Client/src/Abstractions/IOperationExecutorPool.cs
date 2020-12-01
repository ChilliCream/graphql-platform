namespace StrawberryShake
{
    public interface IOperationExecutorPool
    {
        IOperationExecutor CreateExecutor(string name);

        IOperationBatchExecutor CreateBatchExecutor(string name);

        IOperationStreamExecutor CreateStreamExecutor(string name);
    }
}
