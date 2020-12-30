namespace StrawberryShake
{
    public interface IValueSerializer
    {
    }

    public interface IValueSerializer<in TData, out TRuntime> : IValueSerializer
    {
        TRuntime Deserialize(TData data);
    }
}
