namespace StrawberryShake
{
    public interface IOperationExecutorFactory
    {
        string Name { get; }

        IOperationExecutor CreateExecutor();
    }
}
