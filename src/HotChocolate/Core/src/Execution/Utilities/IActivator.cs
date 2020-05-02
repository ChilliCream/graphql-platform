namespace HotChocolate.Execution.Utilities
{
    public interface IActivator
    {
        TResolver GetOrCreateResolver<TResolver>();

        T CreateInstance<T>();
    }
}
