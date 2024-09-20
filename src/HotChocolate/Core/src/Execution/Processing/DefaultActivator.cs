using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Processing;

internal sealed class ResolverProvider : IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _instances = new();
    private bool _disposed;

    public T GetResolver<T>(IServiceProvider services)
    {
        var service = services.GetService<T>();

        if (service is not null)
        {
            return service;
        }

        return (T)_instances.GetOrAdd(typeof(T), CreateResolver);

        object CreateResolver(Type key)
            => ActivatorUtilities.CreateInstance(services, key);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var instance in _instances.Values)
        {
            if (instance is IDisposable d)
            {
                d.Dispose();
            }
        }
        _disposed = true;
    }
}
