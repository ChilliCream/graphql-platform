using Microsoft.Extensions.ObjectPool;

namespace GreenDonut;

internal static class BatchPool<TKey> where TKey : notnull
{
    public static ObjectPool<Batch<TKey>> Shared { get; } = Create();

    private static ObjectPool<Batch<TKey>> Create()
        => new DefaultObjectPool<Batch<TKey>>(
            new BatchPooledObjectPolicy<TKey>(),
            Environment.ProcessorCount * 4);
}
