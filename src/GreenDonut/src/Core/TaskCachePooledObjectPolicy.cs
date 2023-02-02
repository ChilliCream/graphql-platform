using Microsoft.Extensions.ObjectPool;

namespace GreenDonut;

internal class TaskCachePooledObjectPolicy : PooledObjectPolicy<TaskCache>
{
    private readonly int _size;

    public TaskCachePooledObjectPolicy(int size)
    {
        _size = size;
    }

    public override TaskCache Create() => new(_size);

    public override bool Return(TaskCache obj)
    {
        obj.Clear();
        return true;
    }
}