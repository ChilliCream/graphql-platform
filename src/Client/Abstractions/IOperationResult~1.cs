namespace StrawberryShake
{
    public interface IOperationResult<T>
        : IOperationResult
    {
        new T Data { get; }
    }
}
