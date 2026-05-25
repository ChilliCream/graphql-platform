using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.DependencyInjection;

internal sealed class PooledServiceFactory<T> : IFactory<T> where T : class
{
    private readonly ObjectPool<T> _objectPool;

    public PooledServiceFactory(ObjectPool<T> objectPool)
    {
        _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
    }

    public T Create() => _objectPool.Get();
}
