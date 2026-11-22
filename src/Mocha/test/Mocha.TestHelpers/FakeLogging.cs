using Microsoft.Extensions.Logging.Testing;

namespace Mocha.TestHelpers;

public static class FakeLogging
{
    public static FakeLoggerProvider CreateProvider<T>()
        => new(FakeLogCollector.Create(new FakeLogCollectorOptions
        {
            FilteredCategories = { typeof(T).FullName! }
        }));

    public static async Task<FakeLogRecord> WaitForLogAsync(
        this FakeLogCollector collector,
        Func<FakeLogRecord, bool> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellation.CancelAfter(timeout);

        await foreach (var record in collector.GetLogsAsync(cancellation.Token))
        {
            if (predicate(record))
            {
                return record;
            }
        }

        throw ThrowHelper.LogStreamCompleted();
    }
}
