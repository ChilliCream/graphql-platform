using System;

namespace HotChocolate.Execution.Processing
{
    public interface IActivator : IDisposable
    {
        T GetOrCreate<T>();

        object? GetOrCreate(Type type);

        T CreateInstance<T>();

        object? CreateInstance(Type type);
    }
}
