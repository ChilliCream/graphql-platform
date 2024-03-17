using System;
using System.Threading.Tasks;

namespace GreenDonut;

public readonly struct BatchJob(Func<ValueTask> batchPromise)
{
    private readonly Func<ValueTask>? _promise = batchPromise;
    
    public ValueTask DispatchAsync() => _promise?.Invoke() ?? default;
}