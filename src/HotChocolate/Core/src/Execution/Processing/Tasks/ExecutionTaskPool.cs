using System.Diagnostics;
using Microsoft.Extensions.ObjectPool;
using static System.Threading.Interlocked;

namespace HotChocolate.Execution.Processing.Tasks;

/// <summary>
///  A pool of objects. Buffers a set of objects to ensure fast, thread safe object pooling
/// </summary>
internal sealed class ExecutionTaskPool<T, TPolicy> : ObjectPool<T>
    where T : class, IExecutionTask
    where  TPolicy : ExecutionTaskPoolPolicy<T>
{
    private readonly ObjectWrapper[] _items;
    private readonly TPolicy _policy;
    private T? _firstItem;

    public ExecutionTaskPool(TPolicy policy, int maximumRetained = 256)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _items = new ObjectWrapper[maximumRetained - 1];
    }

    /// <summary>
    ///  Gets an object from the buffer if one is available, otherwise get a new buffer
    ///  from the pool one.
    /// </summary>
    /// <returns>A <see cref="ResolverTask"/>.</returns>
    public override T Get()
    {
        var item = _firstItem;
        if (item == null || CompareExchange(ref _firstItem, null, item) != item)
        {
            var items = _items;
            for (var i = 0; i < items.Length; i++)
            {
                item = items[i].Element;
                if (item != null &&
                    CompareExchange(ref items[i].Element, null, item) == item)
                {
                    return item;
                }
            }

            item = _policy.Create(this);
        }

        return item;
    }

    /// <summary>
    ///  Return an object from the buffer if one is available. If the buffer is full
    ///  return the buffer to the pool
    /// </summary>
    public override void Return(T obj)
    {
        if (_policy.Reset(obj))
        {
            if (_firstItem != null || CompareExchange(ref _firstItem, obj, null) != null)
            {
                var items = _items;
                for (var i = 0;
                    i < items.Length && CompareExchange(ref items[i].Element, obj, null) != null;
                    ++i)
                {
                }
            }
        }
    }

    // PERF: the struct wrapper avoids array-covariance-checks from the runtime
    // when assigning to elements of the array.
    [DebuggerDisplay("{Element}")]
    private struct ObjectWrapper
    {
        public T? Element;
    }
}
