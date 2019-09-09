namespace StrawberryShake
{
    public interface IOperationResult<out T>
        : IOperationResult
    {
        new T Data { get; }
    }
}
