using System;

namespace HotChocolate.Execution.Utilities
{
    public interface IActivator
    {
        TResolver GetOrCreateResolver<TResolver>();

        T CreateInstance<T>();

        object CreateInstance(Type type);
    }
}
