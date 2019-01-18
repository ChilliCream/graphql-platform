namespace HotChocolate.Execution
{
    public interface IActivator
    {
        TResolver GetOrCreateResolver<TResolver>();

        T CreateInstance<T>();
    }
}
