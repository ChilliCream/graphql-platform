namespace StrawberryShake
{
    public interface IValueSerializerResolver
    {
        IValueSerializer<TData, TRuntime> GetValueSerializer<TData, TRuntime>(string name);
    }
}
