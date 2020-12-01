namespace StrawberryShake
{
    public interface IOperationStreamExecutorFactory
    {
        string Name { get; }

        IOperationStreamExecutor CreateStreamExecutor();
    }
}
