using System;

namespace HotChocolate.Execution.Processing
{
    public interface IActivator : IDisposable
    {
        T GetOrCreate<T>(IServiceProvider services);

        object? GetOrCreate(Type type, IServiceProvider services);

        T CreateInstance<T>(IServiceProvider services);

        object? CreateInstance(Type type, IServiceProvider services);
    }
}
