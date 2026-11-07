using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.TestHelpers;

/// <summary>
/// A fallback scheduled message store that records what it is given instead of delivering it.
/// </summary>
public sealed class RecordingScheduledMessageStore : IScheduledMessageStore
{
    public const string TokenPrefix = "recording:";

    public ConcurrentQueue<MessageEnvelope> Persisted { get; } = [];

    public ValueTask<string> PersistAsync(IDispatchContext context, CancellationToken cancellationToken)
    {
        Persisted.Enqueue(context.Envelope!);
        return ValueTask.FromResult($"{TokenPrefix}{Persisted.Count}");
    }

    public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
        => ValueTask.FromResult(false);
}

public static class RecordingScheduledMessageStoreExtensions
{
    /// <summary>
    /// Registers <paramref name="store"/> as the fallback scheduled message store.
    /// </summary>
    public static IServiceCollection AddRecordingSchedulingFallback(
        this IServiceCollection services,
        RecordingScheduledMessageStore store)
    {
        services.AddSingleton(store);
        services.AddSingleton(
            new ScheduledMessageStoreRegistration(
                transportType: null,
                RecordingScheduledMessageStore.TokenPrefix,
                typeof(RecordingScheduledMessageStore),
                isFallback: true));

        return services;
    }
}
