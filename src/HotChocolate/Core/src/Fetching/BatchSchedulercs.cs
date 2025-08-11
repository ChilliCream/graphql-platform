using System.Diagnostics;
using GreenDonut;

namespace HotChocolate.Fetching;

/// <summary>
/// The execution engine batch dispatcher.
/// </summary>
public sealed partial class BatchDispatcher
{
    public void Schedule(Batch batch)
    {
        ArgumentNullException.ThrowIfNull(batch, nameof(batch));

        Interlocked.Increment(ref _enqueueVersion);
        Interlocked.Increment(ref _openBatches);

        lock (_batches)
        {
            _batches.Add(batch);
            _lastEnqueued = Stopwatch.GetTimestamp();
        }

        Send(BatchDispatchEventType.Enqueued);
    }
}
