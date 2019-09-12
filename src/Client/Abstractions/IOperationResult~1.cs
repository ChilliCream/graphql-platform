namespace StrawberryShake
{
    public interface IOperationResult<out T>
        : IOperationResult
        where T : class
    {
        new T? Data { get; }
    }
}
