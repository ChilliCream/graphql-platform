using System.Diagnostics;
using GreenDonut;

namespace HotChocolate.Fetching;

public sealed partial class BatchDispatcher
{
    /// <summary>
    /// <para>
    /// Schedules a batch for execution. The batch will be queued and processed
    /// by the batch dispatcher using an intelligent coordination strategy that
    /// prioritizes batches based on their modification timestamp to ensure
    /// optimal batching efficiency.
    /// </para>
    /// </summary>
    /// <param name="batch">
    /// The batch containing one or more data loading keys that should be
    /// scheduled for coordinated execution.
    /// </param>
    public void Schedule(Batch batch)
    {
        ArgumentNullException.ThrowIfNull(batch, nameof(batch));

        lock (_enqueuedBatches)
        {
            if (!_enqueuedBatches.Add(batch))
            {
                throw new ArgumentException("The batch was already enqueued before.");
            }
        }

        Interlocked.Increment(ref _openBatches);
        Volatile.Write(ref _lastEnqueued, Stopwatch.GetTimestamp());

        Send(BatchDispatchEventType.Enqueued);
    }
}
