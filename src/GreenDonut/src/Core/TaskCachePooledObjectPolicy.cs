using Microsoft.Extensions.ObjectPool;

namespace GreenDonut;

internal class TaskCachePooledObjectPolicy(int size) : PooledObjectPolicy<TaskCache>
{
    public override TaskCache Create() => new(size);

    public override bool Return(TaskCache obj)
    {
        obj.Clear();
        return true;
    }
}