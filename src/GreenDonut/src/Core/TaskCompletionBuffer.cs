using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GreenDonut
{
    internal class TaskCompletionBuffer<TKey, TValue>
        : ConcurrentDictionary<TKey, TaskCompletionSource<TValue>>
    { }
}
