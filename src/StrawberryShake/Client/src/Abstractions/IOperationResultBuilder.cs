namespace StrawberryShake
{
    public interface IOperationResultBuilder<in TData, out TResult>
    {
        TResult Build(TData data);
    }
}
